using Microsoft.Extensions.Logging;
using PizzeriaApp.Services;
using PizzeriaApp.Views;
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

            // 1. Capa de Infraestructura (Nuestra fábrica de conexiones REST)
            builder.Services.AddSingleton<SupabaseClientFactory>();

            // 2. Vistas (Registramos Login para poder inyectarle dependencias si lo requiere a futuro)
            builder.Services.AddTransient<Login>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}