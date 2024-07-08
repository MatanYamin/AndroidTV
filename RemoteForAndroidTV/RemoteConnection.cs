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
    private bool _isSendingPong = false;
    private readonly ConcurrentQueue<Func<Task>> _operationQueue = new ConcurrentQueue<Func<Task>>();
    private bool _isProcessingQueue = false;
    private readonly object _queueLock = new object();

    public delegate void NotifyEventHandler(object? sender, EventArgs e);
    public static event NotifyEventHandler ConnectionSuccessEvent, ConnectionLostEvent;

    private IValues PLATFORM_VALUES = default!;
    private static SslStream? _sslStream = default!;
    private static TcpClient? _client = default!;
    const int SEND_COMMANDS_PORT = 6466;
    private readonly string SERVER_IP;
    private CancellationTokenSource _pingCancellationTokenSource = default!;
    private Task? _listeningTask = null;

    public RemoteConnection(string ip)
    {
        this.SERVER_IP = ip;
        AssignPlatformValues();
        Console.WriteLine($"Initialized RemoteConnection with IP: {ip}");
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
        Console.WriteLine($"Assigned platform values: {PLATFORM_VALUES.GetType().Name}");
    }

    private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        return true;
    }

    public async Task ConnectToDevice()
    {
        try
        {
            // Console.WriteLine("Starting ConnectToDevice...");
            X509Certificate2 clientCertificate = await Task.Run(() => 
            {
                Console.WriteLine("Loading client certificate...");
                return new X509Certificate2(SharedPref.LoadClientCertificate());
            });

            _client = new TcpClient();
            Console.WriteLine("Connecting to server...");
            await _client.ConnectAsync(SERVER_IP, SEND_COMMANDS_PORT);

            _sslStream = new SslStream(_client.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate));
            Console.WriteLine("Authenticating as client...");
            await _sslStream.AuthenticateAsClientAsync(SERVER_IP, new X509Certificate2Collection(clientCertificate), false);

            Console.WriteLine("Reading initial server message...");
            await ReadServerMessage();

            byte[] configMess = buildConfigMess();
            Console.WriteLine("Sending config message...");
            await SendServerMessage(configMess);

            Console.WriteLine("Reading server message...");
            await ReadServerMessage();

            Console.WriteLine("Sending second payload...");
            await SendServerMessage(Values.RemoteConnect.SecondPayload);

            Console.WriteLine("Reading server messages (3 times)...");
            for (int i = 0; i < 3; i++)
            {
                await ReadServerMessage();
            }

            Console.WriteLine("Notifying connection success...");
            NotifyConnectionSuccess();
            Console.WriteLine("Starting to listen for pings...");
            await StartListeningForPings();

        }
        catch (SocketException ex)
        {
            Console.WriteLine($"SocketException in ConnectToDevice: {ex.Message}");
            CloseConnection();
            NotifyConnectionLost();
        }
        catch (IOException ex)
        {
            Console.WriteLine($"IOException in ConnectToDevice: {ex.Message}");
            CloseConnection();
            NotifyConnectionLost();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in ConnectToDevice: {ex.Message}");
            CloseConnection();
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
            Console.WriteLine("Reading server message...");
            byte[] buffer = new byte[4096];
            int bytesRead = await _sslStream.ReadAsync(buffer, 0, buffer.Length);

            if (bytesRead > 0)
            {
                byte[] responseData = new byte[bytesRead];
                Array.Copy(buffer, responseData, bytesRead);
                Console.WriteLine("Received server message.");
                return responseData;
            }
            else
            {
                Console.WriteLine("No bytes read from server.");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in ReadServerMessage: {ex.Message}");
            return null;
        }
    }

    private static async Task SendServerMessage(byte[] message)
    {
        byte[] messageLengthArray = { (byte)message.Length };
        try
        {
            Console.WriteLine("Sending server message...");
            await _sslStream.WriteAsync(messageLengthArray, 0, messageLengthArray.Length);
            await Task.Delay(100);
            await _sslStream.WriteAsync(message, 0, message.Length);
            await _sslStream.FlushAsync();
            Console.WriteLine("Server message sent.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in SendServerMessage: {ex.Message}");
        }
    }

 public void SendRemoteButton(byte[] length, byte[] command)
{
    EnqueueOperation(async () =>
    {
        try
        {
            await _sslStream.WriteAsync(length, 0, length.Length);
            await Task.Delay(100);
            await _sslStream.WriteAsync(command, 0, command.Length);
            await _sslStream.FlushAsync();
        }
        catch (IOException ex)
        {
            Console.WriteLine($"IOException in SendRemoteButton: {ex.Message}");
            await ReconnectAndRetry(length, command);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in SendRemoteButton: {ex.Message}");
            await ReconnectAndRetry(length, command);
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
            await WaitForResponse(_sslStream, cancellationToken);
            // Add a small delay to prevent a tight loop
            await Task.Delay(100, cancellationToken);
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Listening for pings was canceled.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Exception in ListenForPings: {ex.Message}");
    }
}


// private async Task WaitForResponse(SslStream sslStream, CancellationToken cancellationToken)
// {
//     byte[] buffer = new byte[4096];
//     try
//     {
//         Console.WriteLine("Waiting for response...");
//         int bytesRead = await sslStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
//         if (bytesRead > 0)
//         {
//             string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
//             byte firstByte = buffer[0];
//             if (firstByte == 8 || firstByte == 66)
//             {
//                 if (!_isSendingPong)
//                 {
//                     _isSendingPong = true;
//                     EnqueueOperation(async () =>
//                     {
//                         try
//                         {
//                             Console.WriteLine("ping is: " + response);
//                             byte[] pongResponse = new byte[] { 74, 2, 8, 25 };
//                             await SendServerMessage(pongResponse);
//                             Console.WriteLine("PONG SENT");
//                         }
//                         finally
//                         {
//                             _isSendingPong = false;
//                         }
//                     });
//                 }
//             }
//             else
//             {
//                 // Handle other types of messages
//                 // Add logic to process the response if needed
//                 Console.WriteLine("1");
//             }
//         }
//         else
//         {
//             Console.WriteLine("2");
//             // CloseConnection();
//             // await ReconnectToDevice();
//         }
//     }
//     catch (OperationCanceledException)
//     {
//         Console.WriteLine("3");
//         // CloseConnection();
//         // await ReconnectToDevice();
//         Console.WriteLine("WaitForResponse operation was canceled.");
//     }
//     catch (Exception ex)
//     {
//         Console.WriteLine("4");
//         // CloseConnection();
//         // await ReconnectToDevice();
//         Console.WriteLine($"Exception in WaitForResponse: {ex.Message}");
//     }
// }

private async Task WaitForResponse(SslStream sslStream, CancellationToken cancellationToken)
{
    byte[] buffer = new byte[128];
    try
    {
        Console.WriteLine("Waiting for response...");
        int bytesRead = await sslStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
        if (bytesRead > 0)
        {

            Console.WriteLine($"Message received: {BitConverter.ToString(buffer)}");

            // string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            byte firstByte = buffer[0];
            Console.Write("buffer is: [");
            foreach(byte b in buffer){
                Console.Write(b + ", ");
            }
            Console.Write("]");
            if (firstByte == 66 || buffer[1] == 66)
            {
                if (!_isSendingPong)
                {
                    _isSendingPong = true;
                    EnqueueOperation(async () =>
                    {
                        try
                        {
                            byte[] pongResponse = new byte[] { 74, 2, 8, 25 };
                            await SendServerMessage(pongResponse);
                            Console.WriteLine("PONG SENT");
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
                // Add logic to process the response if needed
                Console.WriteLine("1");
            }
        }
        else
        {
            Console.WriteLine("2");
            // CloseConnection();
            // await ReconnectToDevice();
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("3");
        // CloseConnection();
        // await ReconnectToDevice();
        Console.WriteLine("WaitForResponse operation was canceled.");
    }
    catch (Exception ex)
    {
        Console.WriteLine("4");
        // CloseConnection();
        // await ReconnectToDevice();
        Console.WriteLine($"Exception in WaitForResponse: {ex.Message}");
    }
}

    private static void NotifyConnectionLost()
    {
        Console.WriteLine("Connection lost.");
        ConnectionLostEvent?.Invoke(null, EventArgs.Empty);
    }

    private static void NotifyConnectionSuccess()
    {
        Console.WriteLine("Connection success.");
        ConnectionSuccessEvent?.Invoke(null, EventArgs.Empty);
    }

    private void CloseConnection()
    {
        try
        {
            Console.WriteLine("Closing connection...");
            _pingCancellationTokenSource?.Cancel();

            if (_listeningTask != null)
            {
                try
                {
                    _listeningTask.Wait();
                }
                catch (AggregateException ex)
                {
                    ex.Handle(inner => inner is OperationCanceledException);
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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in CloseConnection: {ex.Message}");
        }
    }

    private async Task ReconnectAndRetry(byte[] length, byte[] command)
    {
        try
        {
            Console.WriteLine("Reconnecting and retrying...");
            await ReconnectToDevice();
            SendRemoteButton(length, command);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in ReconnectAndRetry: {ex.Message}");
        }
    }

    private async Task ReconnectToDevice()
    {
        CloseConnection();
        await ConnectToDevice();
    }

    private async Task ProcessQueue()
    {
        while (true)
        {
            if (_operationQueue.TryDequeue(out var operation))
            {
                try
                {
                    Console.WriteLine("Processing queue operation...");
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
                    Console.WriteLine("Queue processing complete.");
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
        // var version = assembly.GetName().Version;
        var version = assembly.GetName().Version;
        appVersion = version.ToString();
        // appVersion = "1.0.0.0";

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

        // SIZE_OF_YOUR_APP_VERSION (if it's 1 number then 1)
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

       public static byte[] ConvertStringToByteArray(string input)
    {
        byte[] byteArray = new byte[input.Length];

        for (int i = 0; i < input.Length; i++)
        {
            byteArray[i] = (byte)input[i];
        }

        return byteArray;
    }

       
}
