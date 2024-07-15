using Microsoft.Maui.Controls;
using System.Linq;

namespace RemoteForAndroidTV
{
      
    public partial class PairAndConnect : ContentPage
    {
        HandlePairing? _pairingHandler;
        HandleConnect? _connectHandler;
        RemoteButtons? _remoteButtons;
        public string? ip, name;

        public PairAndConnect(DeviceInfo deviceInfo)
        {
            InitializeComponent();

            Init(deviceInfo);

            HandleLastRemoteConnect();
        }

        void Init(DeviceInfo deviceInfo){

            this.ip = deviceInfo.IpAddress;
            this.name = deviceInfo.Name;

        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            Device.BeginInvokeOnMainThread(() => {
                SingleEntry.Focus();
            });
        }

        private void OnEntryTextChanged(object sender, TextChangedEventArgs e)
        {
            _pairingHandler?.HandleOnEntryTextChanged(sender, e);
        }

        public int GetTVcodeCount(){
            string enteredCode = SingleEntry.Text;
            return enteredCode.Length;
        }

        public string GetTvCodeString(){
            // string enteredCode = $"{Entry1.Text}{Entry2.Text}{Entry3.Text}{Entry4.Text}{Entry5.Text}{Entry6.Text}";
            string enteredCode = SingleEntry.Text;
            return enteredCode;
        }

        private void OnOkButtonClicked(object sender, EventArgs e)
        {
            PopupOverlay.IsVisible = true;
            _pairingHandler?.HandleOnOkButtonClicked(sender, e);
        }

        public async void ConnectionSuccess(RemoteConnection remote, RemoteState remoteState){

            SaveLastRemote();

            // create new instance of a remote buttons with the connection we just made
            _remoteButtons = new RemoteButtons(remote);

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Navigation.PushAsync(new MainRemote(_remoteButtons, remoteState));
            });
        }

        public async void ConnectionFailed()
        {
            RemoveKeyFromSave();
            // Ensure navigation happens on the main thread
            await MainThread.InvokeOnMainThreadAsync( () =>
            {
                Navigation.PopToRootAsync();
            });
        }

        void SaveLastRemote(){
            SharedPref.SaveLastRemote(this.ip, this.name);
        }

        public void RemoveKeyFromSave(){
            SharedPref.RemoveKey(this.ip);
        }

        private void StartPairing(){
            _pairingHandler = new HandlePairing(this);
        }

        public void StartConnecting(){
            _connectHandler = new HandleConnect(this);
        }

        void HandleLastRemoteConnect(){

            bool didConnect = DidConnectedBefore(this.ip);

            if(!didConnect){
                // This is the proccess from scratch
                StartPairing();
                return;
            }
            else{
                var info = SharedPref.GetNickname(this.ip);
                // This is to connect for controling the remote
                StartConnecting();
            }
        }

        bool DidConnectedBefore(string ip){

            return SharedPref.DidConnectedWithIP(ip);
        }

        private void OnPopupOkClicked(object sender, EventArgs e)
        {
            string input = PopupEntry.Text;
            DisplayAlert("Input", $"You entered: {input}", "OK");
            PopupOverlay.IsVisible = false;

            SharedPref.SaveNickname(this.ip, input);

        }

    }
}
