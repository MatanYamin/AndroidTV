using Microsoft.Maui.Controls;
using System.Linq;

namespace RemoteForAndroidTV
{
      
    public partial class PairAndConnect : ContentPage
    {
        HandlePairing? _pairingHandler = null;
        HandleConnect? _connectHandler = null;
        RemoteButtons? _remoteButtons = null;
        MainRemote? _mainRemote = null;
        public DeviceInfo? _device;
        bool alreadyConnected = false;

        public PairAndConnect(DeviceInfo deviceInfo)
        {
            InitializeComponent();

            Init(deviceInfo);

            HandleLastRemoteConnect();

        }

        void Init(DeviceInfo deviceInfo){

            this._device = deviceInfo;

            alreadyConnected = false;

            RemoveLastRemote();

            HideReconnectOverlay();

            // if there was a pairing process before
            if(_pairingHandler != null){
                _pairingHandler.CloseConnectino();
            }

            // has nickname
            if(SharedPref.HasNickName(this._device.IpAddress)){
                ShowHideNickNamePopUp(false);
            }
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

            HideLoading();
            SaveLastRemote();
            SingleEntry.Text = string.Empty;

            // if(alreadyConnected){return;}
            // alreadyConnected = true;

            _remoteButtons = new RemoteButtons(remote);
            _mainRemote = new MainRemote(_remoteButtons, remoteState);

            if(!SharedPref.HasNickName(this._device.IpAddress)){
                ShowHideNickNamePopUp(true);
                return;
                // The moving to the remote page will be after setting nickname
            }

            else{
                MoveToMainRemote();
            }
            
        }

        private async void MoveToMainRemote(){

            Console.WriteLine("moving to main remote");
            await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Navigation.PushAsync(_mainRemote);
                });

        }

        private void ShowHideNickNamePopUp(bool show){
            PopupOverlay.IsVisible = show;
        }

        public void ConnectionFailed()
        {
            Console.WriteLine("CONNEDTED FAILEDDDD");

            HideLoading();

            // RemoveDidConnect();
            RemoveLastRemote();

            ShowReconnectOverlay();
            // _mainRemote.ConnectionFailedShowReConnectButton();
            // await Navigation.PopAsync();

            // Ensure navigation happens on the main thread
            // await MainThread.InvokeOnMainThreadAsync( () =>
            // {
            //     Navigation.PopToRootAsync();
            // });
        }

        void SaveLastRemote(){
            SharedPref.ConnectedSuccess(this._device.IpAddress, this._device.Name);
        }

        public void RemoveDidConnect(){
            SharedPref.RemoveDidConnect(this._device.IpAddress);
        }

        private void RemoveLastRemote(){

            SharedPref.RemoveLastRemote();

        }

        private void StartPairing(){
            _pairingHandler = new HandlePairing(this);
        }

        public void StartConnecting(){
            ShowLoading();
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
            string nickName = PopupEntry.Text;
            
            ShowHideNickNamePopUp(false);

            SharedPref.SaveNickname(this._device.IpAddress, nickName);

            ChangeNicknameInList(nickName);

            MoveToMainRemote();

        }

        void ChangeNicknameInList(string nickName){
            _device.Name = nickName;
        }

        private void ShowLoading()
        {
            LoadingOverlay.IsVisible = true;
        }

        private void HideLoading()
        {
            LoadingOverlay.IsVisible = false;
        }

        private void OnReconnectButtonClicked(object sender, EventArgs e)
        {
            HideReconnectOverlay();
            StartPairing();
            // Add any additional logic for reconnecting here
        }

        public void ShowReconnectOverlay()
        {
            alreadyConnected = false;
            SingleEntry.Text = string.Empty;
            ReconnectOverlay.IsVisible = true;
        }

        public void HideReconnectOverlay()
        {
            SingleEntry.Text = string.Empty;
            ReconnectOverlay.IsVisible = false;
        }

    }
}
