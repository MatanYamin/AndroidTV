using System.Collections.ObjectModel;


public interface INearbyDevicesFinder
{
    Task StartDevicesFindingAsync();
    Task StopDevicesFindingAsync();
}