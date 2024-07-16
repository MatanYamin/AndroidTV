using System.Globalization;
using System.Resources;

namespace RemoteForAndroidTV
{
    public static class ResourceProvider
    {
        private static readonly ResourceManager ResourceManager = new ResourceManager("RemoteForAndroidTV.Resources.Translations.lang", typeof(ResourceProvider).Assembly);

        public static string GetString(string key)
        {
            return ResourceManager.GetString(key, CultureInfo.CurrentUICulture);
            
        }
    }
}
