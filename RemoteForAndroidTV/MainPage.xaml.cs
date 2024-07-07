using Microsoft.Maui.Controls;

namespace RemoteForAndroidTV
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Automatically navigate to DiscoveryPage when MainPage appears
            await Navigation.PushAsync(new DiscoveryDevicesPage());
            // await Navigation.PushAsync(new MainRemote("10.100.102.7"));

            // await Navigation.PushAsync(new EnterCodePage());
        }

    }
}
