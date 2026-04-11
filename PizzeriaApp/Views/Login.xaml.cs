using PizzeriaApp.Models;
using PizzeriaApp.GoogleAuth;
using PizzeriaApp.Services;
using PizzeriaApp.Views;
using PizzeriaApp.Controllers;

namespace PizzeriaApp.Views;

// Esta es la puerta de entrada a la app (Vista); delega la lógica al AuthController
public partial class Login : ContentPage
{
    private AuthController _controller;

    public Login(IGoogleAuthService googleAuthService)
    {
        InitializeComponent();
        // Inicializamos el controlador pasándole las dependencias necesarias
        _controller = new AuthController(googleAuthService, new DataBaseServices());
    }

    // El evento principal: delegamos toda la orquestación al controlador
    private async void OnGoogleLoginClicked(object sender, EventArgs e)
    {
        try
        {
            // El controlador se encarga de hablar con Google y Supabase
            var perfil = await _controller.LoginConGoogleAsync();

            if (perfil == null) return; // Se canceló el login

            // Capturamos el token para notificaciones (lógica de plataforma)
            await CapturarYGuardarTokenAsync(perfil.Id);

            // Mandamos una notificación de bienvenida a través del servicio
            _ = NotificationService.NotificarBienvenidaAsync(new DataBaseServices(), perfil.Id, perfil.Nombre, perfil.EsAdmin);

            // La vista sólo se encarga de la navegación final
            Application.Current.MainPage = new MainNavigation(perfil);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error de Sesión", $"Ocurrió un problema: {ex.Message}", "Ok");
        }
    }

    // Captura el token FCM y le pide al controlador que lo guarde
    private async Task CapturarYGuardarTokenAsync(string userId)
    {
        try
        {
#if ANDROID
            string? fcmToken = PizzeriaApp.Platforms.Android.PizzeriaFirebaseMessagingService.LastKnownToken;

            if (!string.IsNullOrEmpty(fcmToken))
            {
                await _controller.GuardarTokenDispositivoAsync(userId, fcmToken);
            }

            // Suscribirse a cambios de token futuros
            PizzeriaApp.Platforms.Android.PizzeriaFirebaseMessagingService.TokenRefreshed += async (nuevoToken) =>
            {
                await _controller.GuardarTokenDispositivoAsync(userId, nuevoToken);
            };
#endif
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FCM Error: {ex.Message}");
        }
    }
}