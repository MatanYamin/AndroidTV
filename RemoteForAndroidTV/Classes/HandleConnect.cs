
using System.Diagnostics;
using RemoteForAndroidTV;

public class HandleConnect{

    RemoteConnection _remoteConnect;
    PairAndConnect _pairAndConnectHandler;
    private readonly string ip;
    int _reconnectAttemps;
    public HandleConnect(PairAndConnect pac){

        this._pairAndConnectHandler = pac;
        this.ip = pac._device.IpAddress;

        _remoteConnect = new RemoteConnection(this.ip, this);

        StartConnection();
    }

    async private void StartConnection(){
        await _remoteConnect.ConnectToDevice();

    }

    async public void ConnectionFailed(bool reconnect = false){

        // if we want to reconnect and the numbers of reconnections is allowed then try.
        // if(reconnect && ++_reconnectAttemps <= Values.RemoteConnect._maxAttempsToConnect){
        //     await ReinitializeConnectionAsync(true, null);
        //     return;
        // }

        // we dont want to reconnect, notify the page that connection failed.
        _reconnectAttemps = 0;
        _pairAndConnectHandler.ConnectionFailed();
    }

    public void ConnectionSuccess(RemoteState remoteState){
        _pairAndConnectHandler.ConnectionSuccess(_remoteConnect, remoteState);
    }

    public async Task ReinitializeConnectionAsync(bool reConnect = true, byte[]? command = null)
    {
        // return;
        // Ensure the current connection is properly closed and disposed
        if (_remoteConnect != null)
        {
            _remoteConnect.Dispose();
        }

        if(!reConnect){
            _remoteConnect?.NotifyConnectionLost();
            return;
        }

        // Create a new instance and initialize it
        _remoteConnect = new RemoteConnection(this.ip, this);

        await _remoteConnect.ConnectToDevice();

        if(command != null){
            _remoteConnect.SendRemoteButton(command);
        }
    }

}
