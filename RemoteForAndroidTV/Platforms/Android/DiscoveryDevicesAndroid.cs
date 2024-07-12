using System.Collections.ObjectModel;
using Zeroconf;
using System.Diagnostics;

namespace RemoteForAndroidTV
{
    public class DiscoveryDevicesAndroid : INearbyDevicesFinder
    {
        IValues values = new AndroidValues();
        private const int RETRY_COUNT = 3; // Number of retries
        private const int SEARCH_DURATION = 3000; // Duration for each search in milliseconds
        private ObservableCollection<DeviceInfo> devices;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool isSearching;

        public DiscoveryDevicesAndroid(ObservableCollection<DeviceInfo> devices)
        {
            this.devices = devices;
            Debug.WriteLine("DISCOVERY_LOG: DiscoveryDevicesAndroid initialized.");
        }

        private async Task DiscoverServicesWithRetries(int retries, CancellationToken cancellationToken)
        {
            Debug.WriteLine("DISCOVERY_LOG: Starting discovery with retries...");
            for (int i = 0; i < retries; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Debug.WriteLine("DISCOVERY_LOG: Discovery cancelled.");
                    break;
                }

                try
                {
                    await DiscoverServices(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("DISCOVERY_LOG: Discovery operation was cancelled.");
                    break;
                }

                Debug.WriteLine("DISCOVERY_LOG: Waiting before next retry...");
                try
                {
                    await Task.Delay(SEARCH_DURATION, cancellationToken); // Wait for the specified duration before retrying
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("DISCOVERY_LOG: Discovery delay was cancelled.");
                    break;
                }
            }
            Debug.WriteLine("DISCOVERY_LOG: Discovery with retries completed.");
        }

        private async Task DiscoverServices(CancellationToken cancellationToken)
        {
            try
            {
                Debug.WriteLine($"DISCOVERY_LOG: Attempting to resolve services with type: {values.ServiceType()}");
                var responses = await ZeroconfResolver.ResolveAsync(values.ServiceType());

                foreach (var resp in responses)
                {
                    Debug.WriteLine($"DISCOVERY_LOG: Found response: {resp.DisplayName}");
                    foreach (var service in resp.Services)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            Debug.WriteLine("DISCOVERY_LOG: Discovery cancelled.");
                            return;
                        }

                        var ipAddress = resp.IPAddresses.FirstOrDefault(); // Get the first IP address
                        if (!string.IsNullOrEmpty(ipAddress))
                        {
                            Debug.WriteLine($"DISCOVERY_LOG: Found service: {service.Key} with IP: {ipAddress}");
                            if (!devices.Any(d => d.Name == resp.DisplayName && d.IP == ipAddress))
                            {
                                devices.Add(new DeviceInfo { Name = resp.DisplayName, IP = ipAddress });
                            }
                        }
                        else
                        {
                            Debug.WriteLine("DISCOVERY_LOG: No IP address found for service.");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("DISCOVERY_LOG: Discovery operation cancelled.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DISCOVERY_LOG: Error discovering services: {ex.Message}");
            }
        }

        public async Task<bool> StartDevicesFindingAsync()
        {
            Debug.WriteLine("DISCOVERY_LOG: Starting devices finding...");
            _cancellationTokenSource = new CancellationTokenSource();
            isSearching = true;
            while (isSearching)
            {
                try
                {
                    await DiscoverServicesWithRetries(RETRY_COUNT, _cancellationTokenSource.Token);
                    Debug.WriteLine("DISCOVERY_LOG: Device finding was successful.");
                    return true; // If discovery is successful
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("DISCOVERY_LOG: Device finding was cancelled.");
                }

                await Task.Delay(5000); // Adjust the delay as needed
            }
            Debug.WriteLine("DISCOVERY_LOG: Device finding was not successful.");
            return false; // If discovery is not successful
        }

        public Task StopDevicesFindingAsync()
        {
            Debug.WriteLine("DISCOVERY_LOG: Stopping devices finding...");
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
