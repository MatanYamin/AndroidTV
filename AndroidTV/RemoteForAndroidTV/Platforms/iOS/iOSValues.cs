namespace RemoteForAndroidTV;
using Foundation;
using UIKit;

public class iOSValues : IValues
{
    string _serviceType = "_androidtvremote2._tcp.";
    string _packageName = NSBundle.MainBundle.BundleIdentifier;

    
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
        var versionCode = NSBundle.MainBundle.InfoDictionary["CFBundleVersion"].ToString();
        int asciiValueSum = 0;

        foreach (var character in versionCode)
        {
            asciiValueSum += (int)character;
        }

        return (byte)asciiValueSum;
    }

        public void ShowKeyboard()
        {
            var window = UIApplication.SharedApplication.KeyWindow;
            var view = window.RootViewController.View.FindFirstResponder();
            if (view != null)
            {
                view.BecomeFirstResponder();
            }
        }


}

public static class UIViewExtensions
    {
        public static UIView FindFirstResponder(this UIView view)
        {
            if (view.IsFirstResponder)
            {
                return view;
            }

            foreach (var subView in view.Subviews)
            {
                var firstResponder = subView.FindFirstResponder();
                if (firstResponder != null)
                {
                    return firstResponder;
                }
            }

            return null;
        }
    }
    