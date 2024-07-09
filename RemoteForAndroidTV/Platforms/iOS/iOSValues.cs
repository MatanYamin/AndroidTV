namespace RemoteForAndroidTV;
using SystemConfiguration;
using CoreFoundation;
using System.Net;

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

}
    