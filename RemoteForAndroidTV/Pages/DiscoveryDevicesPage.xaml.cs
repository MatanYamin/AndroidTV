using System.Collections.ObjectModel;
using System.Diagnostics;


namespace RemoteForAndroidTV
{
    public partial class DiscoveryDevicesPage : ContentPage
    {
        HandleDiscoveryDevices _handleDiscovery;

        public DiscoveryDevicesPage()
        {
            InitializeComponent();
            _handleDiscovery = new HandleDiscoveryDevices(this);
        }

        public void AssingUIDevices(ObservableCollection<DeviceInfo> devices){
            DevicesListView.ItemsSource = devices;
        }

        private void OnDeviceTapped(object sender, ItemTappedEventArgs e)
        {
            _handleDiscovery.ItemTapped(e);
        }
    }

    public class DeviceInfo
    {
        public required string Name { get; set; }
        public required string IP { get; set; }
    }
}
