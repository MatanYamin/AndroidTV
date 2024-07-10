using Microsoft.Maui.Controls;
using System.Linq;

namespace RemoteForAndroidTV
{
      
    public partial class EnterCodePage : ContentPage
    {
        Pairing _pairing;
        string ip;
        MainRemote _remote = null;


        byte[]? _clientCertificate;
        public EnterCodePage(DeviceInfo deviceInfo)
        {

            InitializeComponent();

            SubscribeEvents();

            this.ip = deviceInfo.IP;

            SharedPref.SaveLastRemote(this.ip, deviceInfo.Name);

            _remote = new MainRemote(ip, this);

            _clientCertificate = null;

            _clientCertificate = TryLoadClientCertificate(ip);

            if(_clientCertificate != null){
                Task.Run(() => _remote.InitializeAsync());
                return;
            }

            _pairing = new Pairing(deviceInfo.IP);
            Task.Run(() => _pairing.StartPairing());

        }

        byte[]? TryLoadClientCertificate(string ip){

            return SharedPref.LoadClientCertificate(ip);
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
            var entry = sender as Entry;

            // Filter out any non-letter and non-digit characters
            if (!string.IsNullOrEmpty(entry.Text))
            {
                string newText = new string(entry.Text.Where(char.IsLetterOrDigit).ToArray()).ToUpper();
                if (entry.Text != newText)
                {
                    entry.Text = newText;
                }
            }
            if (entry.Text.Length == 1)
            {
                FocusNextEntry(entry);
            }
            else if (string.IsNullOrEmpty(entry.Text))
            {
                FocusPreviousEntry(entry);
            }
        }

        private void FocusNextEntry(Entry currentEntry)
        {
            if (currentEntry == Entry1) Entry2.Focus();
            else if (currentEntry == Entry2) Entry3.Focus();
            else if (currentEntry == Entry3) Entry4.Focus();
            else if (currentEntry == Entry4) Entry5.Focus();
            else if (currentEntry == Entry5) Entry6.Focus();
        }

        private void FocusPreviousEntry(Entry currentEntry)
        {
            if (currentEntry == Entry2) Entry1.Focus();
            else if (currentEntry == Entry3) Entry2.Focus();
            else if (currentEntry == Entry4) Entry3.Focus();
            else if (currentEntry == Entry5) Entry4.Focus();
            else if (currentEntry == Entry6) Entry5.Focus();
        }

        private async void OnOkButtonClicked(object sender, EventArgs e)
        {
            // Concatenate the text from all entry fields
            string enteredCode = $"{Entry1.Text}{Entry2.Text}{Entry3.Text}{Entry4.Text}{Entry5.Text}{Entry6.Text}";

            await _pairing.ConnectWithCode(enteredCode);

            await _remote.InitializeAsync();
        }

        public async void ConnectionSuccess(object sender, EventArgs e){
            // UnSubscribeEvents();
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Navigation.PushAsync(_remote);
            });
        } 

        public async void ConnectionLost(object sender, EventArgs e)
        {
            // Ensure navigation happens on the main thread
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                UnSubscribeEvents();
                SharedPref.RemoveKey(this.ip);
                await Navigation.PopToRootAsync();
            });
        }     

        void SubscribeEvents(){

            UnSubscribeEvents();
            RemoteConnection.ConnectionSuccessEvent += ConnectionSuccess;
            RemoteConnection.ConnectionLostEvent += ConnectionLost;
            Pairing.ConnectionLostEvent += ConnectionLost;
        }

        void UnSubscribeEvents(){

            RemoteConnection.ConnectionSuccessEvent -= ConnectionSuccess;
            RemoteConnection.ConnectionLostEvent -= ConnectionLost;
            Pairing.ConnectionLostEvent -= ConnectionLost;

        }

    }
}
