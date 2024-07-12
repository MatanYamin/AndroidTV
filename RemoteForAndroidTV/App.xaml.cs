
using CommunityToolkit.Mvvm.Messaging;

namespace RemoteForAndroidTV
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Initiate this to start the internet checking in the background
            _ = new AppInitializer();

            MainPage = new AppShell();
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

            // WeakReferenceMessenger.Default.Send(new AppEnteredForegroundMessage());

            // Notify when the app enters the foreground
            MessagingCenter.Send(this, "AppEnteredForeground");
        }
    }
}
