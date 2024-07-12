using Microsoft.Maui.Controls;
using System.Linq;

namespace RemoteForAndroidTV
{
      
    public partial class PairAndConnect : ContentPage
    {
        // Pairing _pairing;
        // RemoteConnection _connecting;
        HandlePairing _pairingHandler;
        HandleConnect _connectHandler;
        RemoteButtons _remoteButtons;
        public string ip, name;

        public PairAndConnect(DeviceInfo deviceInfo)
        {
            InitializeComponent();

            Init(deviceInfo);

            HandleLastRemoteConnect();
        }

        void Init(DeviceInfo deviceInfo){

            this.ip = deviceInfo.IP;
            this.name = deviceInfo.Name;

            Console.WriteLine("NAME " + this.name);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Device.BeginInvokeOnMainThread(() => {
                Entry1.Focus();
            });
        }

        private void OnEntryTextChanged(object sender, TextChangedEventArgs e)
        {
            _pairingHandler.HandleOnEntryTextChanged(sender, e);
        }

        public int GetTVcodeCount(){
            string enteredCode = $"{Entry1.Text}{Entry2.Text}{Entry3.Text}{Entry4.Text}{Entry5.Text}{Entry6.Text}";
            return enteredCode.Length;
        }

        public string GetTvCodeString(){
            string enteredCode = $"{Entry1.Text}{Entry2.Text}{Entry3.Text}{Entry4.Text}{Entry5.Text}{Entry6.Text}";
            return enteredCode;
        }

        public void FocusNextEntry(Entry currentEntry)
        {
            if (currentEntry == Entry1) Entry2.Focus();
            else if (currentEntry == Entry2) Entry3.Focus();
            else if (currentEntry == Entry3) Entry4.Focus();
            else if (currentEntry == Entry4) Entry5.Focus();
            else if (currentEntry == Entry5) Entry6.Focus();
        }

        public void FocusPreviousEntry(Entry currentEntry)
        {
            if (currentEntry == Entry2) Entry1.Focus();
            else if (currentEntry == Entry3) Entry2.Focus();
            else if (currentEntry == Entry4) Entry3.Focus();
            else if (currentEntry == Entry5) Entry4.Focus();
            else if (currentEntry == Entry6) Entry5.Focus();
        }

        private void OnOkButtonClicked(object sender, EventArgs e)
        {
            _pairingHandler.HandleOnOkButtonClicked(sender, e);
        }

        public async void ConnectionSuccess(RemoteConnection remote){

            SaveLastRemote();

            // create new instance of a remote buttons with the connection we just made
            _remoteButtons = new RemoteButtons(remote);

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Navigation.PushAsync(new MainRemote(_remoteButtons));
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
                // This is to connect for controling the remote
                StartConnecting();
            }
        }

        bool DidConnectedBefore(string ip){

            return SharedPref.DidConnectedWithIP(ip);
        }
    }
}
