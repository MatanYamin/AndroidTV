namespace RemoteForAndroidTV;
using SystemConfiguration;
using CoreFoundation;
using System.Net;
using Foundation;

public class iOSValues : IValues
{

    public string PackageName()
    {
        return Foundation.NSBundle.MainBundle.BundleIdentifier;
    }

    public string ServiceType()
    {
        return "_androidtvremote2._tcp.";
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
    