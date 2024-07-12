namespace RemoteForAndroidTV;
using System.Collections.ObjectModel;
using System.Diagnostics;
public class HandleDiscoveryDevices{

    private int countPermissionCheckTimes = 0;
    private INearbyDevicesFinder? _deviceFinder;
    private ObservableCollection<DeviceInfo> Devices { get; set; } = [];
    private readonly DiscoveryDevicesPage _discoveryPage = default!;

    public HandleDiscoveryDevices(DiscoveryDevicesPage discoveryPage){
        
        this._discoveryPage = discoveryPage;
        InitFunctions();
    }

    private void InitFunctions(){

        AssignUiDevicesList();
        
        // Assign current platform for later use
        GetPlatform();

        // Initiate search
        SearchDevices();

        // When user returns to the app we get notified
        SubscribeToOnResume();

        LastRemoteState();
    }


    private void AssignUiDevicesList(){
        _discoveryPage?.AssingUIDevices(Devices);
    }

    private void SubscribeToOnResume(){

        MessagingCenter.Subscribe<App>(this, "AppEnteredForeground", (sender) =>
        {
            if (Devices.Count > 0){return;}
            SearchDevices();
        });

    }

    private void GetPlatform()
    {
        #if IOS
            _deviceFinder = new DiscoveryDevicesiOS(Devices);
        #elif ANDROID
            _deviceFinder = new DiscoveryDevicesAndroid(Devices);
        #endif
    }

    private async void SearchDevices()
    {

        bool isPermissionGranted = await (_deviceFinder?.StartDevicesFindingAsync() ?? Task.FromResult(false));

        if (isPermissionGranted)
        {
            // if i want to do something
        }
        else
        {

            if(SharedPref.IsFirstTimeInApp() && ++countPermissionCheckTimes == 1){return;}

            bool goToSettings = await _discoveryPage.DisplayAlert("Permission Denied", "Local network access is required to discover devices. Please enable it in settings.", "Go to Settings", "Cancel");
            if (goToSettings)
            {
                RedirectToSettings();
            }
        }
    }

    public void RedirectToSettings()
    {
        _deviceFinder?.RedirectToSettings();
    }

    public void ItemTapped(ItemTappedEventArgs e){

        if (e.Item != null && e.Item is DeviceInfo deviceInfo)
        {
            MoveToConnectionPage(deviceInfo);
        }
    }

    private void LastRemoteState(){

        string lastRemoteIp = SharedPref.GetLastRemoteIP();
        string lastRemoteName = SharedPref.GetLastRemoteName();

        // If Exists than move to the connection part
        if (!string.IsNullOrEmpty(lastRemoteIp))
        {
            var info = new DeviceInfo { Name = lastRemoteName, IP = lastRemoteIp };
            MoveToConnectionPage(info);
            return;
        }

    }

    private async void MoveToConnectionPage(DeviceInfo info){
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await _discoveryPage.Navigation.PushAsync(new PairAndConnect(info));
        });
    }
    
}