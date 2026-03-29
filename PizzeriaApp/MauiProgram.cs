using Microsoft.Extensions.Logging;
using PizzeriaApp.Views;
using Supabase;

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
            var url = "https://aggsgpvobhnrbpwxhdor.supabase.co";
            var key = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImFnZ3NncHZvYmhucmJwd3hoZG9yIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzQ0NDUxMzYsImV4cCI6MjA5MDAyMTEzNn0.vl5wlweNtIZF01FQlZnb_zgRqEvtAu7IrgbAiV7q8Aw";
            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = true,
            };

            var supabase = new Supabase.Client(url, key, options);
            builder.Services.AddSingleton(supabase);
            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddTransient<Login>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
