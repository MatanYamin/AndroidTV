using System.Collections.ObjectModel;

namespace RemoteForAndroidTV
{
    public partial class DiscoveryDevicesPage : ContentPage
    {
        private INearbyDevicesFinder? _deviceFinder;
        public ObservableCollection<DeviceInfo> Devices { get; set; } = new ObservableCollection<DeviceInfo>();

        public DiscoveryDevicesPage()
        {
            InitializeComponent();
            GetNearbyDevicesFinder();
            DevicesListView.ItemsSource = Devices;

            string lastRemoteIp = SharedPref.GetLastRemoteIP();
            string lastRemoteName = SharedPref.GetLastRemoteName();

            if(!string.IsNullOrEmpty(lastRemoteIp)){
                var info = new DeviceInfo { Name = lastRemoteName, IP = lastRemoteIp };
                MoveNextStep(info);
            }
        }

        async void MoveNextStep(DeviceInfo info){
               await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Navigation.PushAsync(new EnterCodePage(info));
            });
        }

        private void GetNearbyDevicesFinder()
        {
            #if IOS
            _deviceFinder = new DiscoveryDevicesiOS(Devices);
            #elif ANDROID
            _deviceFinder = new DiscoveryDevicesAndroid(Devices);
            #endif
        }

        private async void OnStartDiscoveryClicked(object sender, EventArgs e)
        {
            Devices.Clear();
            await (_deviceFinder?.StartDevicesFindingAsync() ?? Task.CompletedTask);
            DiscoveryStatusLabel.Text = "Discovery running...";
        }

        private async void OnStopDiscoveryClicked(object sender, EventArgs e)
        {
            await (_deviceFinder?.StopDevicesFindingAsync() ?? Task.CompletedTask);
            DiscoveryStatusLabel.Text = "Discovery stopped";
        }

        private async void OnDeviceTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item != null && e.Item is DeviceInfo deviceInfo)
            {
                await Navigation.PushAsync(new EnterCodePage(deviceInfo));
            }
        }
    }

    public class DeviceInfo
    {
        public required string Name { get; set; }
        public required string IP { get; set; }
    }
}
