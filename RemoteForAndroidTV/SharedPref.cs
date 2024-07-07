
public static class SharedPref
{
    public static void SaveClientCertificate(byte[] content)
    {
        string base64String = Convert.ToBase64String(content);
        Preferences.Set("clientCertificate", base64String);
    }

   public static byte[]? LoadClientCertificate()
    {
        string base64String = Preferences.Get("clientCertificate", string.Empty);
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
}