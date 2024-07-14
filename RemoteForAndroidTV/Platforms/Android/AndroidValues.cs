namespace RemoteForAndroidTV;
using Android.Content;
using Android.Views.InputMethods;
using Microsoft.Maui.Controls;
using RemoteForAndroidTV;

public class AndroidValues : IValues
{
    string _serviceType = "_androidtvremote2._tcp.local.";
    string _packageName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;

    public string PackageName()
    {
        return _packageName;
    }

    public string ServiceType()
    {
        return _serviceType;
    }

    public bool IsConnectedToInternet()
    {
        var current = Connectivity.NetworkAccess;
        return current == NetworkAccess.Internet;
    }

    public byte GetVersionCode()
    {
        var context = Android.App.Application.Context;
        var versionCode = context.PackageManager.GetPackageInfo(context.PackageName, 0).LongVersionCode.ToString();
        int asciiValueSum = 0;

        foreach (var character in versionCode)
        {
            asciiValueSum += (int)character;
        }

        return (byte)asciiValueSum;
    }

    public void ShowKeyboard()
    {
        var context = Android.App.Application.Context;
        var inputMethodManager = (InputMethodManager)context.GetSystemService(Context.InputMethodService);
        var activity = Platform.CurrentActivity;
        var token = activity.CurrentFocus?.WindowToken;

        if (token != null)
        {
            inputMethodManager.ToggleSoftInput(ShowFlags.Forced, 0);
        }
        else
        {
            var view = new Android.Views.View(context);
            view.LayoutParameters = new Android.Views.ViewGroup.LayoutParams(0, 0);
            activity.AddContentView(view, view.LayoutParameters);
            view.RequestFocus();
            inputMethodManager.ShowSoftInput(view, ShowFlags.Forced);
        }
    }

}
    