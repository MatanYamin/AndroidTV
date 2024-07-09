namespace RemoteForAndroidTV;
using System.Diagnostics;
using Android;
using Android.Content.PM;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Android.OS;

public class AndroidValues : IValues
{
    public string PackageName()
    {
        return System.Diagnostics.Process.GetCurrentProcess().ProcessName;
    }

    public string ServiceType()
    {
        return "_androidtvremote2._tcp.local.";
    }


public bool IsConnectedToInternet()
{
    var current = Connectivity.NetworkAccess;
    return current == NetworkAccess.Internet;
}


}
    