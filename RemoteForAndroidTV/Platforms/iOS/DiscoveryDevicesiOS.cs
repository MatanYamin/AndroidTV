using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Maui.Dispatching;
using UIKit;

namespace RemoteForAndroidTV
{
    public class DiscoveryDevicesiOS : INearbyDevicesFinder
    {
        IValues values = new iOSValues();
        private BonjourServiceDiscovery bonjourServiceDiscovery;

        int repetitions = 100, delayBetweenSearches = 2000;
        public DiscoveryDevicesiOS(ObservableCollection<DeviceInfo> devices)
        {
            bonjourServiceDiscovery = new BonjourServiceDiscovery();
            bonjourServiceDiscovery.OnServiceFound += async (name, ip) =>
            {
                // Log the discovered service
                
                // Ensure UI updates are on the main thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (!devices.Any(d => d.Name == name && d.IpAddress == ip))
                    {
                        devices.Add(new DeviceInfo { Name = name, IpAddress = ip });
                    }
                });

                await Task.CompletedTask;
            };
        }

        public void RedirectToSettings()
        {
            var url = new NSUrl("app-settings:");
            if (UIApplication.SharedApplication.CanOpenUrl(url))
            {
                UIApplication.SharedApplication.OpenUrl(url, new UIApplicationOpenUrlOptions(), null);
            }
        }

        public async Task<bool> StartDevicesFindingAsync()
        {

            for (int i = 0; i < repetitions; i++)
            {
                bool result = await StartDevicesFindingAsync2();
                Console.WriteLine($"Discovery attempt {i + 1} {(result ? "succeeded" : "failed")}");
                await Task.Delay(delayBetweenSearches);

                // Optionally, you might want to stop the discovery before starting it again
                await StopDevicesFindingAsync();
            }
            return true;
        }

        public async Task<bool> StartDevicesFindingAsync2()
        {
            return await bonjourServiceDiscovery.StartDiscoveryAsync(values.ServiceType());
        }

        public async Task StopDevicesFindingAsync()
        {
            await bonjourServiceDiscovery.StopDiscoveryAsync();
        }
    }
}
