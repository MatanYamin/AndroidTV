using Microsoft.Maui.Controls;

namespace RemoteForAndroidTV
{
    public partial class PairingDevicePage : ContentPage
    {
        Pairing _pairing;
        string ip;

        public PairingDevicePage(DeviceInfo deviceInfo)
        {
            InitializeComponent();
            DeviceNameLabel.Text = deviceInfo.Name;
            DeviceIPLabel.Text = deviceInfo.IP;
            _pairing = new Pairing(deviceInfo.IP);
            this.ip = deviceInfo.IP;
             Task.Run(() => _pairing.StartPairing());
        }

        private async void OnStartPairing(object sender, EventArgs e)
        {
            // await _pairing.StartPairing();
        }

        private async void OnEnterCode(object sender, EventArgs e)
        {
            string enteredCode = InputEntry.Text;

            await _pairing.ConnectWithCode(enteredCode);

            await Task.Delay(1500);

            await Navigation.PushAsync(new MainRemote(ip));

        }
        
    }
}
