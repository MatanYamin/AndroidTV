namespace RemoteForAndroidTV;


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
}
    