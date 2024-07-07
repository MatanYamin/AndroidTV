using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Maui.Dispatching;

namespace RemoteForAndroidTV
{
    public class DiscoveryDevicesiOS : INearbyDevicesFinder
    {
        IValues values = new iOSValues();
        private BonjourServiceDiscovery bonjourServiceDiscovery;

        public DiscoveryDevicesiOS(ObservableCollection<DeviceInfo> devices)
        {
            bonjourServiceDiscovery = new BonjourServiceDiscovery();
            bonjourServiceDiscovery.OnServiceFound += async (name, ip) =>
            {
                // Log the discovered service
                
                // Ensure UI updates are on the main thread
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (!devices.Any(d => d.Name == name && d.IP == ip))
                    {
                        devices.Add(new DeviceInfo { Name = name, IP = ip });
                    }
                });

                await Task.CompletedTask;
            };
        }

        public async Task StartDevicesFindingAsync()
        {
            await bonjourServiceDiscovery.StartDiscoveryAsync(values.ServiceType());
        }

        public async Task StopDevicesFindingAsync()
        {
            await bonjourServiceDiscovery.StopDiscoveryAsync();
        }
    }
}
