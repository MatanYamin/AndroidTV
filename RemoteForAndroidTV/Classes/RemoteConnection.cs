namespace RemoteForAndroidTV;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Text;
using System.Reflection;
using System.Collections.Concurrent;

  public class RemoteState
    {
        public required string IsOn { get; set; }
        public required string VolumeLevel { get; set; }
    }

public class RemoteConnection
{
    int attempsForRecconecting;
    const int SEND_COMMANDS_PORT = 6466;
    private bool _disposed, _isSendingPong, _isProcessingQueue;
    private readonly ConcurrentQueue<Func<Task>> _operationQueue = new ConcurrentQueue<Func<Task>>();
    private readonly object _queueLock = new();
    private static IValues PLATFORM_VALUES = default!;
    private static SslStream? _sslStream = default!;
    private static TcpClient? _client = default!;
    private readonly string SERVER_IP;
    private CancellationTokenSource _pingCancellationTokenSource = default!;
    private Task? _listeningTask = null;
    HandleConnect _connectHandler;

    private byte volState, isOnState = 1;


    public delegate void RemoteStateChanged(string value);
    // Define events
    public static event RemoteStateChanged? VolumeChangedEvent;
    public static event RemoteStateChanged? IsOnChangedEvent;

    public RemoteConnection(string ip, HandleConnect hc)
    {
        _connectHandler = hc;
        this.SERVER_IP = ip;
        AssignPlatformValues();
    }

    private void AssignPlatformValues()
    {
        PLATFORM_VALUES = PlatformManager.GetPlatformValues();
    }

    private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        return true;
    }

    public async Task ConnectToDevice()
    {
        try
        {
            await Task.Delay(100);

            // Get client certificate
            X509Certificate2 clientCertificate = new X509Certificate2(SharedPref.LoadClientCertificate(this.SERVER_IP));

            // create client connection
            _client = new TcpClient();
            await _client.ConnectAsync(SERVER_IP, SEND_COMMANDS_PORT);

            // Authenticate
            _sslStream = new SslStream(_client.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate));
            await _sslStream.AuthenticateAsClientAsync(SERVER_IP, new X509Certificate2Collection(clientCertificate), false);

            // We should get a reponse from the server
            bool answer = await ReadServerMessage();
            if(!answer){return;}

            // Sending the config message
            byte[] configMess = buildConfigMess();
            answer = await SendServerMessage(configMess);
            if(!answer){return;}

            // We should get a reponse from the server
            answer = await ReadServerMessage();
            if(!answer){return;}

            // Sending last payload
            answer = await SendServerMessage(Values.RemoteConnect.SecondPayload);
            if(!answer){return;}
            // await ReadServerMessage();

            // The server should send 3 messages
            for (int i = 0; i < 3; i++)
            {
                answer = await ReadServerMessage();
                if(!answer){return;}
            }

            _ = StartListeningForPings();

            NotifyConnectionSuccess();

        }
        catch(Exception){
            ConnectionFailed(true, null);
        }
        // catch (SocketException ex)
        // {
        //     Console.WriteLine($"SocketException in ConnectToDevice: {ex.Message}");
        //     NotifyConnectionLost(true);
        // }
        // catch (IOException ex)
        // {
        //     Console.WriteLine($"IOException in ConnectToDevice: {ex.Message}");
        //     NotifyConnectionLost(true);
        // }
        // catch (Exception ex)
        // {
        //     Console.WriteLine($"Exception in ConnectToDevice: {ex.Message}");
        //     NotifyConnectionLost(true);
        // }
      
    }

    void PrintBuffer(byte[] buffer)
    {
        string result = "START BUFFER: [" + string.Join(", ", buffer) + "]";
        Console.WriteLine(result);
    }

    private async Task<bool> ReadServerMessage()
    {
        if(!PLATFORM_VALUES.IsConnectedToInternet()){return false;}

        try
        {
            byte[] buffer = new byte[1];
            int bytesRead = await _sslStream.ReadAsync(buffer, 0, buffer.Length);
            byte[] buffer2 = new byte[buffer[0]];
            int bytesRead2 = await _sslStream.ReadAsync(buffer2, 0, buffer2.Length);

            if (bytesRead2 > 0)
            {
                HandleRemoteStateChangeServer(buffer2);
                // PrintBuffer(buffer);
                return true;
            }
            else
            {
                ConnectionFailed(true, null);
                return false;
            }
        }
        catch (Exception)
        {
            ConnectionFailed(true, null);
            return false;
        }
    }

    private async Task<bool> SendServerMessage(byte[] message)
    {
        if(!PLATFORM_VALUES.IsConnectedToInternet()){return false;}

        byte[] messageLengthArray = [(byte)message.Length];
        try
        {
            await _sslStream.WriteAsync(messageLengthArray, 0, messageLengthArray.Length);
            await Task.Delay(100);
            await _sslStream.WriteAsync(message, 0, message.Length);

            await _sslStream.FlushAsync();

            return true;
        }

        catch (Exception)
        {
            ConnectionFailed(true, null);

            return false;
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
            catch(Exception){
                Console.WriteLine("Error in send button message");
                ConnectionFailed(true, command);
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
            while (!cancellationToken.IsCancellationRequested && PLATFORM_VALUES.IsConnectedToInternet())
            {
                await WaitForPings(_sslStream, cancellationToken);
                // Add a small delay to prevent a tight loop
                await Task.Delay(100, cancellationToken);
            }
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
        byte[] sizeOfMEssageBuffer = new byte[1];
        try
        {
            int bytesRead = await sslStream.ReadAsync(sizeOfMEssageBuffer, 0, sizeOfMEssageBuffer.Length, cancellationToken);
            byte[] messageFromServer = new byte[sizeOfMEssageBuffer[0]];
            int bytesRead2 = await sslStream.ReadAsync(messageFromServer, 0, messageFromServer.Length, cancellationToken);
            if (bytesRead2 > 0)
            {

                byte firstByte = messageFromServer[0];
                byte secondByte = messageFromServer[1];
                if (firstByte == 66 || secondByte == 66 || firstByte == 8 || secondByte == 8)
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

                    HandleRemoteStateChangeServer(messageFromServer);
                }
            }
            else
            {
                // Treat zero bytes read as a connection close
                Console.WriteLine("Zero bytes read from the server, connection closed.");
                ConnectionFailed();
                throw new IOException("Connection closed by the server.");
            }
        }
        catch(Exception){
            ConnectionFailed();
        }
        // catch (OperationCanceledException)
        // {
        //     Console.WriteLine("WaitForPings operation was canceled.");
        //     throw;
        // }
        // catch (IOException ex)
        // {
        //     Console.WriteLine($"IOException in WaitForPings: {ex.Message}");
        //     throw; // Re-throw the exception to handle it in ListenForPings
        // }
        // catch (Exception ex)
        // {
        //     Console.WriteLine($"Exception in WaitForPings: {ex.Message}");
        //     throw;
        // }
    }

    public void ConnectionFailed(bool reconnect = false, byte[]? command = null)
    {
        CloseConnection();

        // If we want to reconnect:
        if(reconnect && attempsForRecconecting++ < Values.RemoteConnect._maxAttempsToConnect){
            Console.WriteLine("doing reconnect for the " + attempsForRecconecting + " times");
            ReConnect(command);
            return;
        }

        attempsForRecconecting = 0;
        Console.WriteLine("finally connection end rom the remote");
        _connectHandler.ConnectionClosed();
        
    }

    private async void ReConnect(byte[]? command = null){

        Dispose();

        await ConnectToDevice();

        // If the connection lost while trying to press a button
        if(command != null){
            SendRemoteButton(command);
        }

    }

    private void NotifyConnectionSuccess()
    {

        var remoteState = GetRemoteCurrentState();


        _connectHandler.ConnectionSuccess(remoteState);

       


    }

    private RemoteState GetRemoteCurrentState(){

        var remoteState = new RemoteState
            {
                IsOn = isOnState.ToString(),
                VolumeLevel = volState.ToString()
            };

        return remoteState;
    }

    private void CloseConnection()
    {
        try
        {
            _pingCancellationTokenSource?.Cancel();

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

        byte m6 = PLATFORM_VALUES.GetVersionCode();  // if the app version is 1 = 49...
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
            CloseConnection(); // Ensure async cleanup is complete
        }
        _disposed = true;
    }

    ~RemoteConnection()
    {
        Dispose(false);
    }


    void HandleRemoteStateChangeServer(byte[] stateBuffer){

        byte firstByte = stateBuffer[0];

        if(firstByte == 146){

            byte tempVol = stateBuffer[stateBuffer.Length - 3];
            if(tempVol == 0 && stateBuffer[stateBuffer.Length - 2] == 0){
                return;
            }
            if(tempVol > 100 || tempVol < 0){return;}

            volState = stateBuffer[stateBuffer.Length - 3];
            VolumeChangedEvent?.Invoke(volState.ToString());

        }
        else if(firstByte == 194){
            isOnState = stateBuffer[stateBuffer.Length-1];
            IsOnChangedEvent?.Invoke(isOnState.ToString());
        }

    }

}
