using Microsoft.Extensions.Logging;
using PizzeriaApp.Services;
using PizzeriaApp.Controllers;
using PizzeriaApp.Views;
using PizzeriaApp.GoogleAuth;
using Syncfusion.Maui.Core.Hosting;
using System.Threading.Tasks;
using PizzeriaApp.Config;

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

            builder.Services.AddSingleton<IGoogleAuthService, GoogleAuthService>();
            builder.Services.AddTransient<Login>();


            // Registro de servicios de Dominio (Arquitectura MVC Limpia)
            builder.Services.AddSingleton<ServicioCatalogo>();
            builder.Services.AddSingleton<ServicioPedidos>();
            builder.Services.AddSingleton<ServicioReportes>();
            builder.Services.AddSingleton<ServicioPerfiles>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Inicialización async de Supabase en segundo plano (no bloqueante al arranque)
            Task.Run(async () =>
            {
                try
                {
                    await SupabaseClientFactory.InitializeAsync(Secretos.SupabaseUrl, Secretos.SupabaseApiKey);
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error inicializando Supabase en MauiProgram: {ex.Message}");
                }
            });

            return app;
        }
    }
}