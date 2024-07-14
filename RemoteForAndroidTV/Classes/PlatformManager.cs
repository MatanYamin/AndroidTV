namespace RemoteForAndroidTV;
using System.Collections.ObjectModel;

public static class PlatformManager {

    private static IValues? PLATFORM_VALUES = null;
    private static INearbyDevicesFinder? PLATFORM_SEARCH = null;


    public static INearbyDevicesFinder GetPlatformSearch(ObservableCollection<DeviceInfo> devices)
    {
        if(PLATFORM_SEARCH != null){return PLATFORM_SEARCH;}

        #if IOS
            PLATFORM_SEARCH = new DiscoveryDevicesiOS(devices);
        #elif ANDROID
            PLATFORM_SEARCH = new DiscoveryDevicesAndroid(devices);
        #endif

        return PLATFORM_SEARCH;
    }

    public static IValues GetPlatformValues(){

        if(PLATFORM_VALUES != null){return PLATFORM_VALUES;}

         #if IOS
            PLATFORM_VALUES = new iOSValues();
        #elif ANDROID
            PLATFORM_VALUES = new AndroidValues();
        #endif

        return PLATFORM_VALUES; 
    }

}