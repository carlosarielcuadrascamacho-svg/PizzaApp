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
        _controller = new AuthController(googleAuthService, new DataBaseServices());
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Secuencia de animaciones de entrada "Fade-In" con ligeros retrasos para efecto premium
        await Task.WhenAll(
            ImgLogo.FadeTo(1, 800, Easing.CubicOut),
            ImgLogo.TranslateTo(0, 0, 800, Easing.CubicOut) // Podríamos inicializarlo con un TranslationY si quisiéramos
        );

        await CardLogin.FadeTo(1, 800, Easing.CubicOut);
        await CardLogin.TranslateTo(0, 0, 800, Easing.CubicOut);
    }

    // El evento principal: delegamos toda la orquestación al controlador
    private async void OnGoogleLoginClicked(object sender, EventArgs e)
    {
        try
        {
            // Mostramos el overlay de carga y deshabilitamos el botón para evitar doble clic
            LoadingOverlay.IsVisible = true;
            BtnGoogle.IsEnabled = false;

            // El controlador se encarga de hablar con Google y Supabase
            var perfil = await _controller.LoginConGoogleAsync();

            if (perfil == null)
            {
                LoadingOverlay.IsVisible = false;
                BtnGoogle.IsEnabled = true;
                return; 
            }

            // Capturamos el token para notificaciones (lógica de plataforma)
            await CapturarYGuardarTokenAsync(perfil.Id);

            // Mandamos una notificación de bienvenida a través del servicio
            _ = NotificationService.NotificarBienvenidaAsync(new DataBaseServices(), perfil.Id, perfil.Nombre, perfil.EsAdmin);

            // La vista sólo se encarga de la navegación final
            Application.Current.MainPage = new MainNavigation(perfil);
        }
        catch (Exception ex)
        {
            LoadingOverlay.IsVisible = false;
            BtnGoogle.IsEnabled = true;
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