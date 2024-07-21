using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;
using RemoteForAndroidTV;

namespace RemoteForAndroidTV;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Call the function to configure platform-specific handlers
        ConfigurePlatformSpecificHandlers(builder);

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static void ConfigurePlatformSpecificHandlers(MauiAppBuilder builder)
    {
#if __IOS__
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler<CustomCapsEntry, iOS.CustomCapsEntryHandler>();
        });
#endif
    }
}
