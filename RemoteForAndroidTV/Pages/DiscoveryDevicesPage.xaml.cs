using System.Collections.ObjectModel;
using System.Diagnostics;

namespace RemoteForAndroidTV
{
    public partial class DiscoveryDevicesPage : ContentPage
    {
        private INearbyDevicesFinder? _deviceFinder;
        public ObservableCollection<DeviceInfo> Devices { get; set; } = new ObservableCollection<DeviceInfo>();

        public DiscoveryDevicesPage()
        {
            InitializeComponent();

            Debug.WriteLine("DISCOVERY_PAGE_LOG: Initializing DiscoveryDevicesPage.");

            GetNearbyDevicesFinder();
            DevicesListView.ItemsSource = Devices;

            string lastRemoteIp = SharedPref.GetLastRemoteIP();
            string lastRemoteName = SharedPref.GetLastRemoteName();

            Debug.WriteLine($"DISCOVERY_PAGE_LOG: Last remote IP: {lastRemoteIp}, Last remote name: {lastRemoteName}");

            if (!string.IsNullOrEmpty(lastRemoteIp))
            {
                var info = new DeviceInfo { Name = lastRemoteName, IP = lastRemoteIp };
                MoveNextStep(info);
                return;
            }

            SearchDevices();

            // Listen for the app entering the foreground
            MessagingCenter.Subscribe<App>(this, "AppEnteredForeground", (sender) =>
            {
                Debug.WriteLine("DISCOVERY_PAGE_LOG: App entered foreground.");

                if (Devices.Count > 0)
                {
                    Debug.WriteLine("DISCOVERY_PAGE_LOG: Devices already found, skipping search.");
                    return;
                }

                SearchDevices();
            });
        }

        async void MoveNextStep(DeviceInfo info)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                Debug.WriteLine($"DISCOVERY_PAGE_LOG: Moving to EnterCodePage with device info: {info.Name}, {info.IP}");
                await Navigation.PushAsync(new EnterCodePage(info));
            });
        }

        private void GetNearbyDevicesFinder()
        {
#if IOS
            _deviceFinder = new DiscoveryDevicesiOS(Devices);
            Debug.WriteLine("DISCOVERY_PAGE_LOG: Initialized iOS device finder.");
#elif ANDROID
            _deviceFinder = new DiscoveryDevicesAndroid(Devices);
            Debug.WriteLine("DISCOVERY_PAGE_LOG: Initialized Android device finder.");
#endif
        }

        private async void OnStartDiscoveryClicked(object sender, EventArgs e)
        {
            Debug.WriteLine("DISCOVERY_PAGE_LOG: Start discovery clicked.");
            Devices.Clear();
            await (_deviceFinder?.StartDevicesFindingAsync() ?? Task.CompletedTask);
            DiscoveryStatusLabel.Text = "Discovery running...";
        }

        private async void SearchDevices()
        {
            Debug.WriteLine("DISCOVERY_PAGE_LOG: Starting device search.");
            // Devices.Clear();
            bool isPermissionGranted = await (_deviceFinder?.StartDevicesFindingAsync() ?? Task.FromResult(false));

            if (isPermissionGranted)
            {
                Debug.WriteLine("DISCOVERY_PAGE_LOG: Device discovery running.");
                DiscoveryStatusLabel.Text = "Discovery running...";
            }
            else
            {
                Debug.WriteLine("DISCOVERY_PAGE_LOG: Permission denied or discovery failed.");
                bool goToSettings = await DisplayAlert("Permission Denied", "Local network access is required to discover devices. Please enable it in settings.", "Go to Settings", "Cancel");
                if (goToSettings)
                {
                    RedirectToSettings();
                }
            }
        }

        public void RedirectToSettings()
        {
#if IOS
            var url = new NSUrl("app-settings:");
            if (UIApplication.SharedApplication.CanOpenUrl(url))
            {
                UIApplication.SharedApplication.OpenUrl(url, new UIApplicationOpenUrlOptions(), null);
            }
#elif ANDROID
            var intent = new Android.Content.Intent(Android.Provider.Settings.ActionApplicationDetailsSettings);
            intent.AddFlags(Android.Content.ActivityFlags.NewTask);
            intent.SetData(Android.Net.Uri.FromParts("package", Android.App.Application.Context.PackageName, null));
            Android.App.Application.Context.StartActivity(intent);
#endif
        }

        private async void OnStopDiscoveryClicked(object sender, EventArgs e)
        {
            Debug.WriteLine("DISCOVERY_PAGE_LOG: Stop discovery clicked.");
            await (_deviceFinder?.StopDevicesFindingAsync() ?? Task.CompletedTask);
            DiscoveryStatusLabel.Text = "Discovery stopped";
        }

        private async void OnDeviceTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item != null && e.Item is DeviceInfo deviceInfo)
            {
                Debug.WriteLine($"DISCOVERY_PAGE_LOG: Device tapped: {deviceInfo.Name}, {deviceInfo.IP}");
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
