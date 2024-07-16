
using System.Text.Json;

public static class SharedPref
{
    static string LAST_REMOTE_IP = "LastRemoteIP";
    static string LAST_REMOTE_NAME = "LastRemoteName";
    static string TIMES_IN_APP = "TimesInApp";
    static string SERVER_CERT = "ServerCertificate";


    public static void SaveClientCertificate(string ip, byte[] clientCertificate)
    {

        // Load existing data
        string json = Preferences.Get(ip, string.Empty);
        IpInfo ipInfo;

        // Check if existing data is in JSON format
        if (IsValidJson(json))
        {
            ipInfo = JsonSerializer.Deserialize<IpInfo>(json);
        }
        else
        {
            ipInfo = new IpInfo();
        }

        // Update client certificate
        string base64String = Convert.ToBase64String(clientCertificate);
        ipInfo.ClientCertificate = base64String;

        // Save updated data
        json = JsonSerializer.Serialize(ipInfo);
        Preferences.Set(ip, json);
    }

    public static void SaveNickname(string ip, string nickname)
    {
        // Load existing data
        string json = Preferences.Get(ip, string.Empty);
        IpInfo ipInfo;

        // Check if existing data is in JSON format
        if (IsValidJson(json))
        {
            ipInfo = JsonSerializer.Deserialize<IpInfo>(json);
        }
        else
        {
            ipInfo = new IpInfo();
        }

        // Update nickname
        ipInfo.Nickname = nickname;

        // Save updated data
        json = JsonSerializer.Serialize(ipInfo);
        Preferences.Set(ip, json);
    }

    public static void test(string ip){

        // Load existing data
        string json = Preferences.Get(ip, string.Empty);
        IpInfo ipInfo;

        // Check if existing data is in JSON format
        if (IsValidJson(json))
        {
            ipInfo = JsonSerializer.Deserialize<IpInfo>(json);
        }
        else
        {
            ipInfo = new IpInfo();
        }

        // Update nickname
        ipInfo.DidConnect = false;

        // Save updated data
        json = JsonSerializer.Serialize(ipInfo);
        Preferences.Set(ip, json);

    }

    private static IpInfo? GetIpInfo(string ip)
    {
        string json = Preferences.Get(ip, string.Empty);
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        if (!IsValidJson(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<IpInfo>(json);
    }

    public static string? GetNickname(string ip)
    {
        var info = GetIpInfo(ip);

        return info?.Nickname;
    }

    public static byte[]? LoadClientCertificate(string ip)
    {
        var info = GetIpInfo(ip);
        return string.IsNullOrEmpty(info?.ClientCertificate) ? null : Convert.FromBase64String(info.ClientCertificate);
    }

    public static void SaveServerCertificate(string content)
    {
        Preferences.Set(SERVER_CERT, content);
    }
    public static string LoadServerCertificate()
    {
        return Preferences.Get(SERVER_CERT, string.Empty);
    }

    // Removes a key (if exists)
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

    public static void ConnectedSuccess(string ip, string name){

        

        SaveLastRemoteIP(ip);
        SaveLastRemoteName(name);
        SaveConnectedSuccess(ip);
    }

    private static void SaveConnectedSuccess(string ip){

        // Load existing data
        string json = Preferences.Get(ip, string.Empty);
        IpInfo ipInfo;

        // Check if existing data is in JSON format
        if (IsValidJson(json))
        {
            ipInfo = JsonSerializer.Deserialize<IpInfo>(json);
        }
        else
        {
            ipInfo = new IpInfo();
        }

        // Update nickname
        ipInfo.DidConnect = true;

        // Save updated data
        json = JsonSerializer.Serialize(ipInfo);
        Preferences.Set(ip, json);

    }

    public static int GetTimesInApp(){
        return Preferences.Get(TIMES_IN_APP, 1);
    }

    // Will get called when the app opens and increase by 1 the number
    public static void EnteringAppSaveCount(){
        int currentTimes = Preferences.Get(TIMES_IN_APP, 0) + 1;
        if(currentTimes > 1000){ // Just not to save too big number
            currentTimes = 2;
        }
        Preferences.Set(TIMES_IN_APP, currentTimes);
    }

    public static bool IsFirstTimeInApp(){
        return Preferences.Get(TIMES_IN_APP, 1) == 1;
    }

    public static bool DidConnectedWithIP(string ip){
        var info = GetIpInfo(ip);
        if(info == null){return false;}
        return info.DidConnect;
    }

    private static bool IsValidJson(string strInput)
    {
        if (string.IsNullOrWhiteSpace(strInput)) return false;

        strInput = strInput.Trim();
        if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || //For object
            (strInput.StartsWith("[") && strInput.EndsWith("]")))   //For array
        {
            try
            {
                var obj = JsonDocument.Parse(strInput);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }

}

public class IpInfo
{
    public string? ClientCertificate { get; set; }
    public string? Nickname { get; set; }
    public bool DidConnect { get; set; } = false;
}