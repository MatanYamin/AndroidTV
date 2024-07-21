namespace RemoteForAndroidTV;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register routes if necessary
        Routing.RegisterRoute(nameof(DiscoveryDevicesPage), typeof(DiscoveryDevicesPage));
    }
}
