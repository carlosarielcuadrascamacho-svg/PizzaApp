using Microsoft.Extensions.Logging;
using PizzeriaApp.Services;
using PizzeriaApp.Views;
using PizzeriaApp.GoogleAuth;
// using Supabase; // Nota: Si no usas el SDK oficial y usas HttpClient, puedes quitar este using.

namespace PizzeriaApp
{
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