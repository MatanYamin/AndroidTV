namespace RemoteForAndroidTV;
using System.Diagnostics;
using Android;
using Android.Content.PM;
using AndroidX.Core.App;
using AndroidX.Core.Content;

public class AndroidValues : IValues
{
    public string PackageName()
    {
        return Process.GetCurrentProcess().ProcessName;
    }

    public string ServiceType()
    {
        return "_androidtvremote2._tcp.local.";
    }



}
    