using Microsoft.Maui.Controls;
using System.Linq;

namespace RemoteForAndroidTV
{
      
    public partial class PairAndConnect : ContentPage
    {
        HandlePairing? _pairingHandler;
        HandleConnect? _connectHandler;
        RemoteButtons? _remoteButtons;
        public DeviceInfo? _device;

        public PairAndConnect(DeviceInfo deviceInfo)
        {
            InitializeComponent();

            Init(deviceInfo);

            HandleLastRemoteConnect();
        }

        void Init(DeviceInfo deviceInfo){
            this._device = deviceInfo;

            // if there was a pairing process before
            if(_pairingHandler != null){
                _pairingHandler.CloseConnectino();
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Device.BeginInvokeOnMainThread(() => {
            //     SingleEntry.Focus();
            // });
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
            // PopupOverlay.IsVisible = true;
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
            SharedPref.ConnectedSuccess(this._device.IpAddress, this._device.Name);
        }

        public void RemoveKeyFromSave(){
            SharedPref.RemoveKey(this._device.IpAddress);
        }

        private void StartPairing(){
            _pairingHandler = new HandlePairing(this);
        }

        public void StartConnecting(){
            _connectHandler = new HandleConnect(this);
        }

        void HandleLastRemoteConnect(){

            bool didConnect = DidConnectedBefore(this._device.IpAddress);

            if(!didConnect){
                // This is the proccess from scratch
                StartPairing();
                return;
            }
            else{
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

            SharedPref.SaveNickname(this._device.IpAddress, input);
            ChangeNicknameInList(input);

        }

        void ChangeNicknameInList(string nickName){
            _device.Name = nickName;
        }

    }
}
