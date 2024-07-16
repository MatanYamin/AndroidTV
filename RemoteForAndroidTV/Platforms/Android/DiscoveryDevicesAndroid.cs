using System.Collections.ObjectModel;
using Zeroconf;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace RemoteForAndroidTV
{
    public class DiscoveryDevicesAndroid : INearbyDevicesFinder
    {
        IValues values = new AndroidValues();
        private const int RETRY_COUNT = 5; // Number of retries
        private const int SEARCH_DURATION = 3000; // Duration for each search in milliseconds
        private ObservableCollection<DeviceInfo> devices;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool isSearching;

        public DiscoveryDevicesAndroid(ObservableCollection<DeviceInfo> devices)
        {
            this.devices = devices;
        }

        private async Task DiscoverServicesWithRetries(int retries, CancellationToken cancellationToken)
        {
            for (int i = 0; i < retries; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    await DiscoverServices(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                try
                {
                    await Task.Delay(SEARCH_DURATION, cancellationToken); // Wait for the specified duration before retrying
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task DiscoverServices(CancellationToken cancellationToken)
        {
            try
            {
                var responses = await ZeroconfResolver.ResolveAsync(values.ServiceType());

                foreach (var resp in responses)
                {
                    foreach (var service in resp.Services)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        var ipAddress = resp.IPAddresses.FirstOrDefault(); // Get the first IP address
                        if (!string.IsNullOrEmpty(ipAddress))
                        {
                            // Check if the device already exists in the collection
                            var existingDevice = devices.FirstOrDefault(d => d.IpAddress == ipAddress);
                            if (existingDevice == null)
                            {
                                // Add the new device to the collection
                                devices.Add(new DeviceInfo { Name = resp.DisplayName, IpAddress = ipAddress });
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation
            }
            catch (Exception ex)
            {
                // Handle other exceptions
            }
        }

        public async Task<bool> StartDevicesFindingAsync()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            isSearching = true;
            while (isSearching)
            {
                try
                {
                    await DiscoverServicesWithRetries(RETRY_COUNT, _cancellationTokenSource.Token);
                    return true; // If discovery is successful
                }
                catch (OperationCanceledException)
                {
                    // Handle cancellation
                }

                await Task.Delay(2500); // Adjust the delay as needed
            }
            return false; // If discovery is not successful
        }

        public Task StopDevicesFindingAsync()
        {
            isSearching = false;
            _cancellationTokenSource?.Cancel();
            return Task.CompletedTask;
        }

        public void RedirectToSettings()
        {
            var intent = new Android.Content.Intent(Android.Provider.Settings.ActionApplicationDetailsSettings);
            intent.AddFlags(Android.Content.ActivityFlags.NewTask);
            intent.SetData(Android.Net.Uri.FromParts("package", Android.App.Application.Context.PackageName, null));
            Android.App.Application.Context.StartActivity(intent);
        }
    }
}
