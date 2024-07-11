using Microsoft.Maui.Controls;
using Microsoft.Maui.LifecycleEvents;
using System;

namespace RemoteForAndroidTV
{
    public partial class App : Application
    {
        private bool _alertDisplayed;

        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();

            StartInternetCheck();
        }

        private void StartInternetCheck()
        {
            Device.StartTimer(TimeSpan.FromSeconds(3), () =>
            {
                CheckInternetConnection();
                return true; // True = Repeat again, False = Stop the timer
            });
        }

        private async void CheckInternetConnection()
        {
            var isConnected = IsConnectedToInternet();
            if (!isConnected && !_alertDisplayed)
            {
                _alertDisplayed = true;
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Application.Current.MainPage.DisplayAlert("No Internet", "You are not connected to the internet", "OK");
                    _alertDisplayed = false;
                });
            }
        }

        private bool IsConnectedToInternet()
        {
            var current = Connectivity.NetworkAccess;
            return current == NetworkAccess.Internet;
        }

        protected override void OnStart()
        {
            base.OnStart();
        }

        protected override void OnSleep()
        {
            base.OnSleep();
        }

        protected override void OnResume()
        {
            base.OnResume();

            // Notify when the app enters the foreground
            MessagingCenter.Send(this, "AppEnteredForeground");
        }
    }
}
