namespace RemoteForAndroidTV;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

public class SendCommands{

    RemoteConnection _remoteConnect = default!;

    public SendCommands(string ip){

        // Creating a connection to the device
        _remoteConnect = new RemoteConnection(ip);
    }

      public async Task InitializeAsync()
    {
        await _remoteConnect.InitializeConnectionAsync();
    }

    public async Task TestChannelUpCommand()
    {
        byte[] m1 = [7];
        byte[] m1press = [82, 5, 8, 166, 1, 16, 3];

        _remoteConnect.SendRemoteButton(m1, m1press);
    }

    public async Task TestVolumeCommand()
    {
        byte[] m1 = [6];
        byte[] m1press = [82, 5, 8, 8, 16, 1];
        byte[] m2 = [6];
        byte[] m2press = [82, 4, 8, 8, 16, 2];

        _remoteConnect.SendRemoteButton(m1, m1press);
        _remoteConnect.SendRemoteButton(m2, m2press);
    }

}