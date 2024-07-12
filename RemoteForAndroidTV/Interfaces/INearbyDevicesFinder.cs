using System.Collections.ObjectModel;


public interface INearbyDevicesFinder
{
    Task<bool> StartDevicesFindingAsync();
    Task StopDevicesFindingAsync();
    void RedirectToSettings();
}