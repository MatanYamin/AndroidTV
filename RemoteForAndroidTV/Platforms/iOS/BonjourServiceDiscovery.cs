using Foundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace RemoteForAndroidTV
{
    public class BonjourServiceDiscovery : NSObject, INSNetServiceBrowserDelegate, INSNetServiceDelegate
    {
        private readonly NSNetServiceBrowser _serviceBrowser;
        public List<(string name, string ip)> DiscoveredServices { get; private set; } = new List<(string name, string ip)>();
        private readonly List<NSNetService> _resolvingServices = new List<NSNetService>();
        private TaskCompletionSource<bool> _tcs;

        public event Func<string, string, Task> OnServiceFound = async (name, ip) => { await Task.CompletedTask; };

        public BonjourServiceDiscovery()
        {
            _serviceBrowser = new NSNetServiceBrowser
            {
                Delegate = this
            };
        }

         public async Task<bool> StartDiscoveryAsync(string serviceType)
        {
            _tcs = new TaskCompletionSource<bool>();
            _serviceBrowser.SearchForServices(serviceType, "local.");
            
            // Timeout to handle permission denial
            await Task.WhenAny(_tcs.Task, Task.Delay(5000));
            
            return _tcs.Task.IsCompleted && _tcs.Task.Result;
        }

        public async Task StopDiscoveryAsync()
        {
            _serviceBrowser.Stop();
            await Task.Delay(2000); // Wait for a short duration to ensure all services are resolved
            _tcs.TrySetResult(true);
        }

        [Export("netServiceBrowser:didFindService:moreComing:")]
        public void DidFindService(NSNetServiceBrowser browser, NSNetService service, bool moreComing)
        {
            service.Delegate = this;
            _resolvingServices.Add(service);
            service.Resolve();
        }

        [Export("netService:didNotResolve:")]
        public void DidNotResolve(NSNetService sender, NSDictionary errors)
        {
            _resolvingServices.Remove(sender);
        }

        [Export("netServiceDidResolveAddress:")]
        public void DidResolveAddress(NSNetService sender)
        {
            foreach (NSData addressData in sender.Addresses)
            {
                var addressBytes = addressData.ToArray();
                if (addressBytes.Length >= 4)
                {
                    var ipAddress = new IPAddress(addressBytes.Skip(4).Take(4).ToArray()).ToString();
                    if (!string.IsNullOrEmpty(ipAddress) && ipAddress != "0.0.0.0")
                    {
                        var serviceInfo = (sender.Name, ipAddress);
                        if (!DiscoveredServices.Any(s => s.name == sender.Name && s.ip == ipAddress))
                        {
                            DiscoveredServices.Add(serviceInfo);
                            OnServiceFound?.Invoke(sender.Name, ipAddress);
                        }
                    }
                }
            }
            _resolvingServices.Remove(sender);
            if (_resolvingServices.Count == 0)
            {
                _tcs.TrySetResult(true);
            }
        }
    }
}
