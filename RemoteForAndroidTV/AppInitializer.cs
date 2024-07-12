using RemoteForAndroidTV;

public class AppInitializer{

    private bool _alertDisplayed;


    public AppInitializer(){
        HandleInitFunctions();
    }

    private void HandleInitFunctions(){

        // Increase entrances to app count by 1.
        SaveAppEntrance();

        // This will initiate internet check every X seconds
        StartInternetCheck(Values._checkinternetConnectionEvery);
    }
    
    public void StartInternetCheck(int checkEvery)
    {
        Device.StartTimer(TimeSpan.FromSeconds(checkEvery), () =>
        {
            HandleinternetConnection();
            return true; // True = Repeat again, False = Stop the timer
        });
    }

    private bool IsConnectedToInternet()
    {
        var current = Connectivity.NetworkAccess;
        return current == NetworkAccess.Internet;
    }

    private async void HandleinternetConnection()
    {
        // Returns false if not connected
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

     public void SaveAppEntrance(){
            // We want to know the number of times the user entered the app.
            SharedPref.EnteringAppSaveCount();
        }

}