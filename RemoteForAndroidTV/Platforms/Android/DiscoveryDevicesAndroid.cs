using System.Collections.ObjectModel;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Zeroconf;
using Android;
using Android.Content.PM;
using AndroidX.Core.App;
using AndroidX.Core.Content;

namespace RemoteForAndroidTV
{
    public class DiscoveryDevicesAndroid : INearbyDevicesFinder
    {
        IValues values = new AndroidValues();
        private const int RETRY_COUNT = 3; // Number of retries
        private const int SEARCH_DURATION = 5000; // Duration for each search in milliseconds
        private ObservableCollection<DeviceInfo> devices;
        private CancellationTokenSource? _cancellationTokenSource;

        public DiscoveryDevicesAndroid(ObservableCollection<DeviceInfo> devices)
        {
            this.devices = devices;
            Console.WriteLine("DiscoveryDevicesAndroid initialized.");
        }

        private async Task DiscoverServicesWithRetries(int retries, CancellationToken cancellationToken)
        {
            Console.WriteLine("Starting discovery with retries...");
            for (int i = 0; i < retries; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Discovery cancelled.");
                    break;
                }

                try
                {
                    await DiscoverServices(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Discovery operation was cancelled.");
                    break;
                }

                Console.WriteLine("Waiting before next retry...");
                try
                {
                    await Task.Delay(SEARCH_DURATION, cancellationToken); // Wait for the specified duration before retrying
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Discovery delay was cancelled.");
                    break;
                }
            }
            Console.WriteLine("Discovery with retries completed.");
        }

        private async Task DiscoverServices(CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine($"Attempting to resolve services with type: {values.ServiceType()}");
                var responses = await ZeroconfResolver.ResolveAsync(values.ServiceType());

                foreach (var resp in responses)
                {
                    foreach (var service in resp.Services)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            Console.WriteLine("Discovery cancelled.");
                            return;
                        }

                        var ipAddress = resp.IPAddresses.FirstOrDefault(); // Get the first IP address
                        if (!string.IsNullOrEmpty(ipAddress))
                        {
                            if (!devices.Any(d => d.Name == resp.DisplayName && d.IP == ipAddress))
                            {
                                devices.Add(new DeviceInfo { Name = resp.DisplayName, IP = ipAddress });
                            }
                            else
                            {
                            }
                        }
                        else
                        {
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Discovery operation cancelled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error discovering services: {ex.Message}");
            }
        }

        public async Task StartDevicesFindingAsync()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            try
            {
                await DiscoverServicesWithRetries(RETRY_COUNT, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Device finding was cancelled.");
            }
        }

        public Task StopDevicesFindingAsync()
        {
           _cancellationTokenSource?.Cancel();
            return Task.CompletedTask;
        }

    }
   
}
