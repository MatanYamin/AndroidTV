using Microsoft.Maui.Controls;

namespace RemoteForAndroidTV
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            MoveToStartPage();

        }

        async void MoveToStartPage(){
            await Navigation.PushAsync(new DiscoveryDevicesPage());
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

        }

    }
}
