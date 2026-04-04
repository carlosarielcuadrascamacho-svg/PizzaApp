using Microsoft.Extensions.Logging;
using PizzeriaApp.Services;
using PizzeriaApp.Views;
using PizzeriaApp.GoogleAuth;
using Syncfusion.Maui.Core.Hosting;

namespace PizzeriaApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureSyncfusionCore()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            builder.Services.AddSingleton<SupabaseClientFactory>();

            builder.Services.AddSingleton<IGoogleAuthService, GoogleAuthService>();
            builder.Services.AddTransient<Login>();


#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}