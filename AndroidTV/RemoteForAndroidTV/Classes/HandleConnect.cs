
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

    public void ConnectionClosed(){

        // we dont want to reconnect, notify the page that connection failed.
        _reconnectAttemps = 0;
        _pairAndConnectHandler.ConnectionFailed();
    }

    public void ConnectionSuccess(RemoteState remoteState){
        _pairAndConnectHandler.ConnectionSuccess(_remoteConnect, remoteState);
    }


}
