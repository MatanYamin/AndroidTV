namespace RemoteForAndroidTV;

public class RemoteButtons{
    RemoteConnection _remote;

    public RemoteButtons(RemoteConnection rc){
        _remote = rc;
    }

    public void TestChannelUpCommand()
    {
        byte[] m1press = [82, 5, 8, 166, 1, 16, 3];

        _remote.SendRemoteButton(m1press);
    }

    public void IncreaseVolume(){

        byte[] m1press = [82, 4, 8, 24, 16, 1];
        _remote.SendRemoteButton(m1press);
        byte[] m2press = [82, 4, 8, 24, 16, 2];
        _remote.SendRemoteButton(m2press);
    }

    public void Digit1(){
        byte[] m1press = [82, 5, 8, 8, 16, 1];
        byte[] m2press = [82, 4, 8, 8, 16, 2];

        _remote.SendRemoteButton(m1press);
        _remote.SendRemoteButton(m2press);
    }

    public void CleanRemote()
    {
        if (_remote != null)
        {
            _remote.Dispose();
            _remote = null;
        }
    }

}