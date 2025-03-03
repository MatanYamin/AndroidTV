using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.VisualBasic;

namespace RemoteForAndroidTV
{
    public partial class DiscoveryDevicesPage : ContentPage
    {
        HandleDiscoveryDevices _handleDiscovery;

        public DiscoveryDevicesPage()
        {

            InitializeComponent();

            // Handle discovery
            _handleDiscovery = new HandleDiscoveryDevices(this);

        }

        // protected override void OnAppearing()
        // {
        //     _handleDiscovery.HandleOnAppearing();
        // }

        public void AssingUIDevices(ObservableCollection<DeviceInfo> devices){
            DevicesListView.ItemsSource = devices;
        }

        private void OnDeviceTapped(object sender, ItemTappedEventArgs e)
        {
            _handleDiscovery.ItemTapped(e);
        }

        void testLang(){
            string greeting = ResourceProvider.GetString("Hello");
            Console.WriteLine(greeting);
        }

    }

  public class DeviceInfo : INotifyPropertyChanged
{
    private string name;
    private string ipAddress;
    private bool lastRemote = false;

    public string Name
    {
        get => name;
        set
        {
            if (name != value)
            {
                name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    public string IpAddress
    {
        get => ipAddress;
        set
        {
            if (ipAddress != value)
            {
                ipAddress = value;
                OnPropertyChanged(nameof(IpAddress));
            }
        }
    }

    public bool LastRemote
    {
        get => lastRemote;
        set
        {
            if (lastRemote != value)
            {
                lastRemote = value;
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

}
