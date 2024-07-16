using System.Collections.ObjectModel;
using System.ComponentModel;
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

  public class DeviceInfo : INotifyPropertyChanged
{
    private string name;
    private string ipAddress;

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

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}


}
