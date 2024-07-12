
using System.Diagnostics;
using RemoteForAndroidTV;

public class HandleConnect{

    RemoteConnection _remoteConnect;
    PairAndConnect _pairAndConnectHandler;
    private readonly string ip;
    public HandleConnect(PairAndConnect pac){

        this._pairAndConnectHandler = pac;
        this.ip = pac.ip;

        _remoteConnect = new RemoteConnection(this.ip, this);

        StartConnection();
    }

    async private void StartConnection(){
        await _remoteConnect.ConnectToDevice();

    }

    public void ConnectionFailed(){
        _pairAndConnectHandler.ConnectionFailed();
    }

    public void ConnectionSuccess(){
        _pairAndConnectHandler.ConnectionSuccess(_remoteConnect);
    }

    public async Task ReinitializeConnectionAsync(bool reConnect = true, byte[]? command = null)
    {
        // Ensure the current connection is properly closed and disposed
        if (_remoteConnect != null)
        {
            _remoteConnect.Dispose();
        }

        if(!reConnect){
            _remoteConnect?.NotifyConnectionLost();
            return;
        }

        Console.WriteLine("reconnect");
        // Create a new instance and initialize it
        _remoteConnect = new RemoteConnection(this.ip, this);

        await _remoteConnect.ConnectToDevice();

        if(command != null){
            _remoteConnect.SendRemoteButton(command);
        }
    }

}
