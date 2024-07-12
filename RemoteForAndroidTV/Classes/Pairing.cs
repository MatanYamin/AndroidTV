namespace RemoteForAndroidTV;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

public class Pairing
{
    // public delegate void NotifyEventHandler(object? sender, EventArgs e);
    // public static event NotifyEventHandler? ConnectionSuccessEvent, ConnectionLostEvent;
    private static SslStream? sslStream = default!;
    private static TcpClient? client = default!;
    const int PAIRING_PORT = 6467;
    private readonly string SERVER_IP;
    HandlePairing _pairinghandler;

    public Pairing(string ip, HandlePairing hp){
        _pairinghandler = hp;
        this.SERVER_IP = ip;
    }

    public async Task StartPairing()
    {

        CloseConnection();

        try
        {
            await Task.Run(async () =>
            {
                // Fetch the server certificate
                await FetchServerCertificate(this.SERVER_IP, PAIRING_PORT);
                // Generate the client certificate
                GenerateClientCertificate();
                // Connect to the server
                await ConnectToServer();
            });
        }
        catch (Exception)
        {
            CloseConnection();
            NotifyConnectionLost();
        }
    }

    public async Task<string?> FetchServerCertificate(string serverIp, int port)
        {
            string serverUrl = $"https://{serverIp}:{port}";

            using var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, certificate, chain, errors) =>
            {

                if (certificate != null)
                {
                    var cert = new X509Certificate2(certificate);
                    string pemCert = ExportToPem(cert);
                    SharedPref.SaveServerCertificate(pemCert);

                    // Return false to stop further processing since we only want the certificate
                    return false;
                }

                Console.WriteLine($"SSL Policy Errors: {errors}");
                return errors == SslPolicyErrors.None;
            };

            using var client = new HttpClient(handler);
            try
            {
                var response = await client.GetAsync(serverUrl);
                // Console.WriteLine($"Making request to {serverUrl}...");
                // var response = await client.GetAsync(serverUrl);
                // No need to process the response since our goal is to fetch the certificate
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                return null;
            }

            return "Certificate retrieved and saved.";
        }

    private static string ExportToPem(X509Certificate2 cert)
    {
        string pemCert = "-----BEGIN CERTIFICATE-----\n"
                        + Convert.ToBase64String(cert.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks)
                        + "\n-----END CERTIFICATE-----\n";
        return pemCert;
    }

    private void GenerateClientCertificate()
    {
        // Distinguished Name details
        var distinguishedName = new X500DistinguishedName("CN=atvremote, C=US, ST=California, L=Mountain View, O=Google Inc., OU=Android, E=example@google.com");

        using (RSA rsa = RSA.Create(2048))
        {
            var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            // Create a self-signed certificate
            var certificate = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(10));

            // Export the certificate and private key
            byte[] certBytes = certificate.Export(X509ContentType.Pfx);

            SharedPref.SaveClientCertificate(this.SERVER_IP, certBytes);
        }
    }

    private static void CloseConnection()
    {
        try
        {
            if (sslStream != null)
            {
                sslStream.Close();
                sslStream.Dispose();
                sslStream = null;
            }

            if (client != null)
            {
                client.Close();
                client.Dispose();
                client = null;
            }

            Console.WriteLine("Connection closed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while closing the connection: {ex.Message}");
        }
    }

    private async Task ConnectToServer()
    {
        try
        {

            byte[]? serverResponse;

            byte[]? certificateContent = SharedPref.LoadClientCertificate(this.SERVER_IP);

            if (certificateContent == null)
            {
                throw new InvalidOperationException("Client certificate content is null.");
            }

            // Load client certificate
            X509Certificate2 clientCertificate = new(certificateContent);

            // Establish a TCP connection
            client = new TcpClient();
            await client.ConnectAsync(SERVER_IP, PAIRING_PORT);
            Console.WriteLine("Successfully connected to the server.");

            // Create SSL stream
            sslStream = new SslStream(client.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate));

            // Authenticate the server and send the client certificate
            await sslStream.AuthenticateAsClientAsync(SERVER_IP, new X509CertificateCollection() { clientCertificate }, false);
            Console.WriteLine("Successfully AuthenticateAsClientAsync to the server.");

            // Send the first set of messages
            await SendServerMessage(Values.Pairing.FirstPayloadMessage);
            serverResponse = await ReadServerMessages(sslStream);
            VerifyResult(serverResponse);

            // Send the next set of messages based on the server response
            await SendServerMessage(Values.Pairing.SecondPayloadMessage);
            serverResponse = await ReadServerMessages(sslStream);
            VerifyResult(serverResponse);

            // Send the last set of messages
            await SendServerMessage(Values.Pairing.ThirdPayloadMessage);
            serverResponse = await ReadServerMessages(sslStream);
            VerifyResult(serverResponse);
         
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Read operation was canceled.");
        }
        catch (IOException ex)
        {
            Console.WriteLine("Disconnected from the server: " + ex.Message);
        }
        catch (Exception ex)
        {
            // Ignore OperationCanceledException to prevent the message "The operation was canceled"
            if (ex is OperationCanceledException)
            {
                Console.WriteLine("Read operation was canceled.");
            }
            else
            {
                Console.WriteLine($"An error occurred: {ex.Message}");

                // Check if there's an inner exception
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
        }
    }

    private static async Task SendServerMessage(byte[] message)
    {
        try
        {
            byte[] messageLengthArray = [(byte)message.Length];

            // Send the message length
            await sslStream.WriteAsync(messageLengthArray, 0, messageLengthArray.Length);
            await Task.Delay(100); // Optional delay to ensure message length is processed separately

            // Send the actual message
            await sslStream.WriteAsync(message, 0, message.Length);
            await sslStream.FlushAsync();

        }
        catch (Exception ex)
        {
            CloseConnection();
            Console.WriteLine($"Exception in SendServerMessage: {ex.Message}");
        }
    }
    
    private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        // Validate the server certificate here
        return true; // For now, we accept any server certificate
    }

    public async Task<bool> ConnectWithCode(string tvCode)
    {
        try
        {
            byte[] encodedSecret = await EncryptSecretAsync(tvCode);

            byte[] encodedSecretNew = [8, 2, 16, 200, 1, 194, 2, 34, 10, 32];
            byte[] concatenatedArray = encodedSecretNew.Concat(encodedSecret).ToArray();

            await SendServerMessage(concatenatedArray);
            byte[]? serverResponse = await ReadServerMessages(sslStream);
            VerifyResult(serverResponse);

            CloseConnection();

            return true;

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in ConnectWithCode: {ex.Message}");
            return false;
        }
    }

    private X509Certificate2 LoadCertificateFromPem(string pem)
    {
        string base64Cert = pem
            .Replace("-----BEGIN CERTIFICATE-----", string.Empty)
            .Replace("-----END CERTIFICATE-----", string.Empty)
            .Replace("\n", string.Empty)
            .Replace("\r", string.Empty)
            .Trim();

        byte[] certBytes = Convert.FromBase64String(base64Cert);
        return new X509Certificate2(certBytes);

    }

    private async Task<byte[]> EncryptSecretAsync(string code)
    {
        return await Task.Run(() =>
        {
            // nonce are the last 4 characters of the code displayed on the TV
            byte[] nonce = FromHexString(code.Substring(2)).ToArray();

            X509Certificate2 clientCertificate = new X509Certificate2(SharedPref.LoadClientCertificate(this.SERVER_IP));
            string pemCert = SharedPref.LoadServerCertificate();
            X509Certificate2 serverCertificate = LoadCertificateFromPem(pemCert);

            // Extract the RSA public key from the loaded certificate
            var clientRSAPublicKey = clientCertificate.GetRSAPublicKey().ExportParameters(false);
            var serverRSAPublicKey = serverCertificate.GetRSAPublicKey().ExportParameters(false);

            // Concatenate components with nonce
            byte[] inputBytes = clientRSAPublicKey.Modulus
                                    .Concat(clientRSAPublicKey.Exponent)
                                    .Concat(serverRSAPublicKey.Modulus)
                                    .Concat(serverRSAPublicKey.Exponent)
                                    .Concat(nonce)
                                    .ToArray();

            // Compute SHA-256 hash
            using (SHA256 sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(inputBytes);
            }
        });
    }

    private static byte[] FromHexString(string hex)
    {
    #if !NETCOREAPP
        byte[] raw = new byte[hex.Length / 2];
        for (int i = 0; i < raw.Length; i++)
        {
            raw[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return raw;
        #else
        return Convert.FromHexString(hex);
        #endif

    }

        public async Task<byte[]?> ReadMessageAsync(Stream networkStream, int messageLen)
        { 
        
            byte[] message = new byte[messageLen];
            int bytesRead = await networkStream.ReadAsync(message, 0, message.Length);

            if(bytesRead > 0){
                return message;
            }

            else{
                NotifyConnectionLost();
                return null;
            }

        }

        private async Task<byte[]?> ReadServerMessages(Stream networkStream)
        {

            byte[]? firstServerMessage = await ReadMessageAsync(networkStream, 1);
            int lengthNextMessage = firstServerMessage[0];

            byte[]? secondMessage = await ReadMessageAsync(networkStream, lengthNextMessage);

            return secondMessage;
    }

     private void NotifyConnectionLost()
    {
        CloseConnection();
        _pairinghandler.ConnectionFailed();
    }

    // private static void NotifyConnectionSuccess()
    // {
    //     ConnectionSuccessEvent?.Invoke(null, EventArgs.Empty);
    // }

    
        private void VerifyResult(byte[]? response)
        {
            if (response == null){
                NotifyConnectionLost();
                throw new ArgumentNullException(nameof(response));
            }

        }
}
