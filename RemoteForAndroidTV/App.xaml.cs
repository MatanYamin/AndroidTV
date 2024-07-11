using Microsoft.Maui.Controls;
using Microsoft.Maui.LifecycleEvents;

namespace RemoteForAndroidTV
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

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

            // Notify when the app enters the foreground
            MessagingCenter.Send(this, "AppEnteredForeground");
        }
    }
}
