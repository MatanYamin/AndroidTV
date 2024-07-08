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

public class RemoteConnection{


    // Define a delegate type for the event
    public delegate void NotifyEventHandler(object? sender, EventArgs e);

    // Declare the static event using the delegate type
    public static event NotifyEventHandler ConnectionSuccessEvent, ConnectionLostEvent;
    private IValues PLATFORM_VALUES = default!;
    private static SslStream _sslStream = default!;
    private static TcpClient _client = default!;
    const int SEND_COMMANDS_PORT = 6466;
    private readonly string SERVER_IP;
    private CancellationTokenSource _pingCancellationTokenSource = default!;

    public RemoteConnection(string ip){

        this.SERVER_IP = ip;
        AssignPlatformValues();
        Task.Run(() => ConnectToDevice());

    }

    private void AssignPlatformValues()
    {
        #if ANDROID
        PLATFORM_VALUES = new AndroidValues();
        #elif IOS
        PLATFORM_VALUES = new iOSValues();
        #endif
    }

    public async Task ConnectToDevice()
    {
        try
        {
            X509Certificate2 clientCertificate = new X509Certificate2(SharedPref.LoadClientCertificate());

            // Create a TCP client
            _client = new TcpClient(SERVER_IP, SEND_COMMANDS_PORT);
            _sslStream = new SslStream(_client.GetStream(), false,
                new RemoteCertificateValidationCallback((sender, certificate, chain, sslPolicyErrors) => true));
            
            // Authenticate as client
            await _sslStream.AuthenticateAsClientAsync(SERVER_IP, new X509Certificate2Collection(clientCertificate), false);

            // Read the server response
            await ReadServerMessage();

            byte[] configMess = buildConfigMess();
            await SendServerMessage(configMess);

            // Read the server response
            await ReadServerMessage();

            await SendServerMessage(Values.RemoteConnect.secondPayload);

            // server should respond with 3 messages
            for (int i = 0; i < 3; i++)
            {
                // Read the server response
                await ReadServerMessage();
            }

            StartListeningForPings();

            NotifyConnectionSuccess();

        }
        catch (Exception ex)
        {
            CloseConnection();
            // NEED TO RETURN TO LIST OF DEVICES TO CONNECT AND TELL THE USER CODE WRONG
            Console.WriteLine($"Exception: {ex.Message}");
            NotifyConnectionLost();
            // await ReconnectToDevice();
        }
    }

    // public async Task ConnectToDevice()
    // {
    //     try
    //     {
    //         X509Certificate2 clientCertificate = new X509Certificate2(SharedPref.LoadClientCertificate());

    //         // Create a TCP client
    //         _client = new TcpClient(SERVER_IP, SEND_COMMANDS_PORT);
    //         _sslStream = new SslStream(_client.GetStream(), false,
    //             new RemoteCertificateValidationCallback((sender, certificate, chain, sslPolicyErrors) => true));
            
    //         // Authenticate as client
    //         await _sslStream.AuthenticateAsClientAsync(SERVER_IP, new X509Certificate2Collection(clientCertificate), false);

    //         byte[] configMess1 = buildConfigMess();
    //         await SendServerMessage(configMess1);

    //         // Console.WriteLine("Sending payload");
    //         await SendServerMessage(Values.RemoteConnect.secondPayload);

    //         StartListeningForPings();

    //     }
    //     catch (Exception ex)
    //     {
    //         CloseConnection();
    //         // NEED TO RETURN TO LIST OF DEVICES TO CONNECT AND TELL THE USER CODE WRONG
    //         Console.WriteLine($"Exception: {ex.Message}");
    //         // await ReconnectToDevice();
    //     }
    // }

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

    private static async Task SendServerMessage(byte[] message)
    {
        byte[] messageLengthArray = [(byte)message.Length];

        try{
         await _sslStream.WriteAsync(messageLengthArray, 0, messageLengthArray.Length);
         await Task.Delay(100);
         await _sslStream.WriteAsync(message, 0, message.Length);
         await _sslStream.FlushAsync();
        }

        catch (Exception ex)
        {
            Console.WriteLine($"Exception in Sending Message: {ex.Message}");
        }
    }

    public async Task SendRemoteButton(byte[] length, byte[] command)
    {
        try
        {
            await _sslStream.WriteAsync(length, 0, length.Length);
            await Task.Delay(100);
            await _sslStream.WriteAsync(command, 0, command.Length);
            await _sslStream.FlushAsync();

        }
        catch (Exception ex)
        {

            Console.WriteLine($"Exception in SendRemoteButton: {ex.Message}");
            await ReconnectAndRetry(length, command);
        }
    }

    private async void StartListeningForPings()
        {
            _pingCancellationTokenSource = new CancellationTokenSource();
            await ListenForPings(_pingCancellationTokenSource.Token);
        }

        private async Task ListenForPings(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await WaitForResponse(_sslStream, cancellationToken);
            }
        }

        private static async Task WaitForResponse(SslStream sslStream, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[4096];
            try
            {
                int bytesRead = await sslStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (bytesRead > 0)
                {
                    // Convert the response to a string and print it
                    string response = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Received {bytesRead} bytes from server: {response}");

                    // Check if the response is a ping packet
                    if (buffer[0] == 66 && buffer[1] == 6)
                    {
                        // Send pong response
                        byte[] pongResponse = [74, 2, 8, 25];
                        await sslStream.WriteAsync(pongResponse, 0, pongResponse.Length, cancellationToken);
                        await sslStream.FlushAsync(cancellationToken);
                        Console.WriteLine("Sent pong response to server.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in WaitForResponse: {ex.Message}");
                // Handle reconnection
            }
        }

        private static void NotifyConnectionLost(){
            Console.WriteLine("connectino lost");
            ConnectionLostEvent?.Invoke(null, EventArgs.Empty);
        }

        private static void NotifyConnectionSuccess(){
            ConnectionSuccessEvent?.Invoke(null, EventArgs.Empty);
        }

        private void CloseConnection()
        {
            try
            {
                _pingCancellationTokenSource?.Cancel();
                _sslStream?.Close();
                _client?.Close();
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
            await ReconnectToDevice();
            await SendRemoteButton(length, command);
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
    private async Task<byte[]?> ReadServerMessage()
    {
        try
        {
            byte[] buffer = new byte[4096];
            int bytesRead = await _sslStream.ReadAsync(buffer, 0, buffer.Length);

            if (bytesRead > 0)
            {
                byte[] responseData = new byte[bytesRead];
                Array.Copy(buffer, responseData, bytesRead);
                return responseData;
            }
            else
            {
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in ReadServerMessage: {ex.Message}");
            return null;
        }
    }

}