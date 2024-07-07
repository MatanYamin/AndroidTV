using Microsoft.Maui.Controls;
using System.Linq;

namespace RemoteForAndroidTV
{
      
    public partial class EnterCodePage : ContentPage
    {
        Pairing _pairing;
        string ip;
        public EnterCodePage(DeviceInfo deviceInfo)
        {
            InitializeComponent();
            
            _pairing = new Pairing(deviceInfo.IP);
            this.ip = deviceInfo.IP;
             Task.Run(() => _pairing.StartPairing());
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

            await Task.Delay(1500);

            await Navigation.PushAsync(new MainRemote(ip));
        }
    }
}
