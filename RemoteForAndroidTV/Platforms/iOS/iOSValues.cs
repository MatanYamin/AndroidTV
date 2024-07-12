namespace RemoteForAndroidTV;
using SystemConfiguration;
using CoreFoundation;
using System.Net;
using Foundation;

public class iOSValues : IValues
{
    string _serviceType = "_androidtvremote2._tcp.";
    string _packageName = NSBundle.MainBundle.BundleIdentifier;

    
    public string PackageName()
    {
        return _packageName;
    }

    public string ServiceType()
    {
        return _serviceType;
    }

    public bool IsConnectedToInternet()
    {
        var current = Connectivity.NetworkAccess;
        return current == NetworkAccess.Internet;
    }

    public byte GetVersionCode()
    {
        var versionCode = NSBundle.MainBundle.InfoDictionary["CFBundleVersion"].ToString();
        int asciiValueSum = 0;

        foreach (var character in versionCode)
        {
            asciiValueSum += (int)character;
        }

        return (byte)asciiValueSum;
    }

}
    