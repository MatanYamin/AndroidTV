
public static class SharedPref
{

    static string LAST_REMOTE_IP = "LastRemoteIP";
    static string LAST_REMOTE_NAME = "LastRemoteName";

    public static void SaveClientCertificate(string ip, byte[] content)
    {
        string base64String = Convert.ToBase64String(content);
        Preferences.Set(ip, base64String);
    }

    public static byte[]? LoadClientCertificate(string ip)
    {
        string base64String = Preferences.Get(ip, string.Empty);
        return string.IsNullOrEmpty(base64String) ? null : Convert.FromBase64String(base64String);
    }

    public static void SaveServerCertificate(string content)
    {
        Preferences.Set("serverCertificate", content);
    }

    public static string LoadServerCertificate()
    {
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

}