namespace RemoteForAndroidTV;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.Maui.Controls;
using System.Net.Sockets;
using System.Diagnostics;
using System.Text;
using System.Reflection;
using System.Collections.Concurrent;

public class RemoteConnection
{
    private bool _disposed = false;
    private bool _isSendingPong = false;
    private readonly ConcurrentQueue<Func<Task>> _operationQueue = new ConcurrentQueue<Func<Task>>();
    private bool _isProcessingQueue = false;
    private readonly object _queueLock = new object();

    public delegate void NotifyEventHandler(object? sender, EventArgs e);
    public static event NotifyEventHandler ConnectionSuccessEvent, ConnectionLostEvent;

    private static IValues PLATFORM_VALUES = default!;
    private static SslStream? _sslStream = default!;
    private static TcpClient? _client = default!;
    const int SEND_COMMANDS_PORT = 6466;
    private readonly string SERVER_IP;
    private CancellationTokenSource _pingCancellationTokenSource = default!;
    private Task? _listeningTask = null;
    SendCommands _sendCommands;

    public RemoteConnection(string ip, SendCommands sc)
    {
        _sendCommands = sc;
        this.SERVER_IP = ip;
        AssignPlatformValues();
    }

    public async Task InitializeConnectionAsync()
    {
        await ConnectToDevice();
    }

    private void AssignPlatformValues()
    {
        #if ANDROID
        PLATFORM_VALUES = new AndroidValues();
        #elif IOS
        PLATFORM_VALUES = new iOSValues();
        #endif
    }

    private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        return true;
    }

    public async Task ConnectToDevice()
    {
        try
        {
            X509Certificate2 clientCertificate = await Task.Run(() => 
            {
                return new X509Certificate2(SharedPref.LoadClientCertificate());
            });

            _client = new TcpClient();
            await _client.ConnectAsync(SERVER_IP, SEND_COMMANDS_PORT);

            _sslStream = new SslStream(_client.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate));
            await _sslStream.AuthenticateAsClientAsync(SERVER_IP, new X509Certificate2Collection(clientCertificate), false);

            await ReadServerMessage();

            byte[] configMess = buildConfigMess();
            await SendServerMessage(configMess);

            await ReadServerMessage();

            await SendServerMessage(Values.RemoteConnect.SecondPayload);
            for (int i = 0; i < 3; i++)
            {
                await ReadServerMessage();
            }

            _ = StartListeningForPings();

            NotifyConnectionSuccess();

        }
        catch (SocketException ex)
        {
            Console.WriteLine($"SocketException in ConnectToDevice: {ex.Message}");
            await CloseConnectionAsync();
            NotifyConnectionLost();
        }
        catch (IOException ex)
        {
            Console.WriteLine($"IOException in ConnectToDevice: {ex.Message}");
            await CloseConnectionAsync();
            NotifyConnectionLost();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in ConnectToDevice: {ex.Message}");
            await CloseConnectionAsync();
            NotifyConnectionLost();
        }
        finally
        {
            Console.WriteLine("Finished ConnectToDevice.");
        }
    }

    private async Task<byte[]?> ReadServerMessage()
    {
        try
        {
            byte[] buffer = new byte[100];
            int bytesRead = await _sslStream.ReadAsync(buffer, 0, buffer.Length);

            if (bytesRead > 0)
            {
                return buffer;
            }
            else
            {
                Console.WriteLine("No bytes read from server.");
                await CloseConnectionAsync();
                NotifyConnectionLost();
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in ReadServerMessage: {ex.Message}");
            await CloseConnectionAsync();
            NotifyConnectionLost();
            return null;
        }
    }

  private async Task SendServerMessage(byte[] message)
{
    byte[] messageLengthArray = [(byte)message.Length];

    try
    {
        await _sslStream.WriteAsync(messageLengthArray, 0, messageLengthArray.Length);
        await Task.Delay(100);
        await _sslStream.WriteAsync(message, 0, message.Length);
        await _sslStream.FlushAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Exception in SendServerMessage: {ex.Message}");

        await CloseConnectionAsync(); // Ensure connection cleanup on error
    }
}


 public void SendRemoteButton(byte[] command)
{

    if(!PLATFORM_VALUES.IsConnectedToInternet()){return;}
    
    byte[] messageLengthArray = [(byte)command.Length];

    EnqueueOperation(async () =>
    {
        try
        {

            await _sslStream.WriteAsync(messageLengthArray, 0, messageLengthArray.Length);
            await Task.Delay(100);
            await _sslStream.WriteAsync(command, 0, command.Length);
            await _sslStream.FlushAsync();

        }
        catch (IOException ex)
        {
            Console.WriteLine($"IOException in SendRemoteButton: {ex.Message}");
            KillAndReConnect(command);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in SendRemoteButton: {ex.Message}");
            KillAndReConnect(command);
        }
    });
}

    private void EnqueueOperation(Func<Task> operation)
    {
        _operationQueue.Enqueue(operation);
        StartQueueProcessor();
    }

    private Task StartListeningForPings()
    {
        _pingCancellationTokenSource = new CancellationTokenSource();
        _listeningTask = ListenForPings(_pingCancellationTokenSource.Token);
        return _listeningTask;
    }


    private async Task ListenForPings(CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine("Listening for pings...");
            while (!cancellationToken.IsCancellationRequested)
            {
                await WaitForPings(_sslStream, cancellationToken);
                // Add a small delay to prevent a tight loop
                await Task.Delay(100, cancellationToken);
            }
            Console.WriteLine("Cancellation requested, exiting ListenForPings loop.");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Listening for pings was canceled.");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"IOException in ListenForPings: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in ListenForPings: {ex.Message}");
        }
        finally
        {
            Console.WriteLine("ListenForPings task is completing.");
        }
    }

    private async Task WaitForPings(SslStream sslStream, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[128];
        try
        {
            int bytesRead = await sslStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
            if (bytesRead > 0)
            {
                byte firstByte = buffer[0];
                byte secondByte = buffer[1];

                if (firstByte == 66 || firstByte == 8 || secondByte == 66 || secondByte == 6)
                {
                    if (!_isSendingPong)
                    {
                        _isSendingPong = true;
                        EnqueueOperation(async () =>
                        {
                            try
                            {
                                await SendServerMessage(Values.RemoteConnect.Pong);
                            }
                            finally
                            {
                                _isSendingPong = false;
                            }
                        });
                    }
                }
                else
                {
                    // Handle other types of messages
                    Console.WriteLine($"Received non-PING message: {Encoding.UTF8.GetString(buffer, 0, bytesRead)}");
                }
            }
            else
            {
                // Treat zero bytes read as a connection close
                Console.WriteLine("Zero bytes read from the server, connection closed.");
                throw new IOException("Connection closed by the server.");
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("WaitForPings operation was canceled.");
            throw;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"IOException in WaitForPings: {ex.Message}");
            throw; // Re-throw the exception to handle it in ListenForPings
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in WaitForPings: {ex.Message}");
            throw;
        }
        finally
        {
            Console.WriteLine("WaitForPings task is completing.");
        }
    }

    private static void NotifyConnectionLost()
    {
        ConnectionLostEvent?.Invoke(null, EventArgs.Empty);
    }

    private static void NotifyConnectionSuccess()
    {
        ConnectionSuccessEvent?.Invoke(null, EventArgs.Empty);
    }

    private async Task CloseConnectionAsync()
    {
        try
        {
            Console.WriteLine("Closing connection...");
            _pingCancellationTokenSource?.Cancel();

            if (_listeningTask != null)
            {
                Console.WriteLine("Waiting for listening task to complete...");
                try
                {
                    await _listeningTask;
                    Console.WriteLine("Listening task completed.");
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("OperationCanceledException while waiting for listening task.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception while waiting for listening task: {ex.Message}");
                }
            }

            if (_sslStream != null)
            {
                _sslStream.Close();
                _sslStream.Dispose();
                _sslStream = null;
            }

            if (_client != null)
            {
                _client.Close();
                _client.Dispose();
                _client = null;
            }

            Console.WriteLine("Connection closed successfully.");
            // killme();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in CloseConnection: {ex.Message}");
        }
    }

    // Executing queue actions
    private async Task ProcessQueue()
    {
        while (true)
        {
            // Dequeue from the queue (if there any)
            if (_operationQueue.TryDequeue(out var operation))
            {
                try
                {
                    // Execute command
                    await operation();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception during operation: {ex.Message}");
                }
            }
            else
            {
                lock (_queueLock)
                {
                    _isProcessingQueue = false;
                    break;
                }
            }
        }
    }

    private void StartQueueProcessor()
    {
        lock (_queueLock)
        {
            if (!_isProcessingQueue)
            {
                _isProcessingQueue = true;
                _ = ProcessQueue();
            }
        }
    }

    public byte[] buildConfigMess()
    {
        // Get the app version number
        string appVersion;
        // Get the package name
        string packageName = PLATFORM_VALUES.PackageName();

        // Get the app version
        var assembly = Assembly.GetExecutingAssembly();

        var version = assembly.GetName().Version;
        appVersion = version.ToString();

        List<byte> myliST = new List<byte>();

        // tag
        byte m1 = 10;
        myliST.Add(m1);

        // SIZE_OF_THE_WHOLE_MESSAGE
        byte m2 = 11; // change after knowing the length => full message length - 2
        myliST.Add(m2);

        // 8, 238, 4, 18
        byte[] m3 = {8, 238, 4, 18};
        myliST.AddRange(m3);

        // SIZE_OF_THE_SUB_MESSAGE
        byte m4 = 12; // change after know the length => full message length - 7
        myliST.Add(m4);

        // 24, 1, 34
        byte[] m4a = {24, 1, 34}; // change after know the length => full message length - 7
        myliST.AddRange(m4a);

        // SIZE_OF_YOUR_APP_VERSION (if it's 1 number than 1)
        byte m5 = 1;
        myliST.Add(m5);

        // YOUR_APP_VERSION_NUMBER: e.g. 1 becomes 49
        byte m6 = 49;  // if the app version is 1 = 49...
        myliST.Add(m6);

        // 42: tag
        byte m6a = 42;
        myliST.Add(m6a);

        // SIZE_OF_PACKAGE_NAME
        byte m7 = (byte)packageName.Length;
        myliST.Add(m7);

        // package name converted from string (ascii) to decimal
        byte[] m8 = ConvertStringToByteArray(packageName);
        myliST.AddRange(m8);

        // 50: tag
        byte m9 = 50;
        myliST.Add(m9);

        // SIZE_OF_APP_VERSION
        byte m10 = (byte)appVersion.Length;
        myliST.Add(m10);

        // APP_VERSION: e.g. 1.0.0 becomes 49, 46, 48, 46, 48
        byte[] m11 = ConvertStringToByteArray(appVersion);
        myliST.AddRange(m11);

        byte totalLen = (byte)myliST.Count();
        // fix the SIZE_OF_THE_WHOLE_MESSAGE according to our created message
        myliST[1] = (byte)(totalLen-2);
        // fix SIZE_OF_THE_SUB_MESSAGE
        myliST[6] = (byte)(totalLen-7);

        byte[] payload = myliST.ToArray();

        return payload;
    }

    // Gets a string and convert it to byte array for sending to server
    public static byte[] ConvertStringToByteArray(string input)
    {
        byte[] byteArray = new byte[input.Length];

        for (int i = 0; i < input.Length; i++)
        {
            byteArray[i] = (byte)input[i];
        }

        return byteArray;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

     protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            CloseConnectionAsync().Wait(); // Ensure async cleanup is complete
        }

        _disposed = true;
    }

    ~RemoteConnection()
    {
        Dispose(false);
    }

    // This closes the connection and reconnect
    async void KillAndReConnect(byte[]? command = null){
       await _sendCommands.ReinitializeConnectionAsync(command);
    }

}
