namespace RemoteForAndroidTV;


public class AndroidValues : IValues
{
    string _serviceType = "_androidtvremote2._tcp.local.";
    string _packageName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;

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
        var context = Android.App.Application.Context;
        var versionCode = context.PackageManager.GetPackageInfo(context.PackageName, 0).LongVersionCode.ToString();
        int asciiValueSum = 0;

        foreach (var character in versionCode)
        {
            asciiValueSum += (int)character;
        }

        return (byte)asciiValueSum;
    }

}
    