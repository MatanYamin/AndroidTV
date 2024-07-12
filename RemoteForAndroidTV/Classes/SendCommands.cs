namespace RemoteForAndroidTV;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

public class SendCommands{

    RemoteConnection? _remoteConnect = default!;
    string ServerIp;

    public SendCommands(string ip){

        this.ServerIp = ip;
        // Creating a connection to the device
        _remoteConnect = new RemoteConnection(ip, this);
    }

      public async Task InitializeAsync()
    {
        await _remoteConnect.InitializeConnectionAsync();
    }

    public void TestChannelUpCommand()
    {
        byte[] m1press = [82, 5, 8, 166, 1, 16, 3];

        _remoteConnect.SendRemoteButton(m1press);
    }

    public void TestVolumeCommand()
    {
        byte[] m1press = [82, 5, 8, 8, 16, 1];
        byte[] m2press = [82, 4, 8, 8, 16, 2];

        _remoteConnect.SendRemoteButton(m1press);
        _remoteConnect.SendRemoteButton(m2press);
    }

public async Task ReinitializeConnectionAsync(bool reConnect = true, byte[]? command = null)
    {
        // Ensure the current connection is properly closed and disposed
        if (_remoteConnect != null)
        {
            _remoteConnect.Dispose();
        }

        if(!reConnect){
            _remoteConnect.NotifyConnectionLost();
            return;
        }

        // Create a new instance and initialize it
        _remoteConnect = new RemoteConnection(ServerIp, this);
        await _remoteConnect.InitializeConnectionAsync();

        if(command != null){
            _remoteConnect.SendRemoteButton(command);
        }
    }

      public async void CleanRemote(){

    Console.WriteLine("0");
        if (_remoteConnect != null)
        {
            _remoteConnect.Dispose();
            _remoteConnect = null;
        }
        Console.WriteLine("1");

    }



}