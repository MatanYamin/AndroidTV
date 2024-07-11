
public static class SharedPref
{

    static string LAST_REMOTE_IP = "LastRemoteIP";
    static string LAST_REMOTE_NAME = "LastRemoteName";
    static string TIMES_IN_APP = "TimesInApp";

    public static void SaveClientCertificate(string ip, byte[] content)
    {
        string base64String = Convert.ToBase64String(content);
        Preferences.Set(ip, base64String);
    }

    public static byte[]? LoadClientCertificate(string ip)
    {
        string base64String = Preferences.Get(ip, string.Empty);
        if(base64String == ""){Console.WriteLine("EMPTY CLIENTTT");}
        return string.IsNullOrEmpty(base64String) ? null : Convert.FromBase64String(base64String);
    }

    public static void SaveServerCertificate(string content)
    {
        Preferences.Set("serverCertificate", content);
    }

    public static string LoadServerCertificate()
    {
        if(Preferences.Get("serverCertificate", string.Empty) == ""){Console.WriteLine("EMPTY SERVERRRR");}
        return Preferences.Get("serverCertificate", string.Empty);
    }

    public static void RemoveKey(string key){
        Preferences.Remove(key);
    }

    private static void SaveLastRemoteIP(string ip){
        Preferences.Set(LAST_REMOTE_IP, ip);
    }
    public static string GetLastRemoteIP(){
        return Preferences.Get(LAST_REMOTE_IP, string.Empty);
    }
    private static void SaveLastRemoteName(string name){
        Preferences.Set(LAST_REMOTE_NAME, name);
    }
    public static string GetLastRemoteName(){
        return Preferences.Get(LAST_REMOTE_NAME, string.Empty);
    }

    public static void SaveLastRemote(string ip, string name){

        SaveLastRemoteIP(ip);
        SaveLastRemoteName(name);
    }

    public static int GetTimesInApp(){
        return Preferences.Get(TIMES_IN_APP, 1);
    }

    public static void EnteringAppSaveCount(){

        int currentTimes = Preferences.Get(TIMES_IN_APP, 0) + 1;
        Preferences.Set(TIMES_IN_APP, currentTimes);

    }

}