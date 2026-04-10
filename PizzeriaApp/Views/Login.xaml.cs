using PizzeriaApp.Models;
using PizzeriaApp.GoogleAuth;
using PizzeriaApp.Services;
using PizzeriaApp.Views;

namespace PizzeriaApp.Views;

public partial class Login : ContentPage
{
    private IGoogleAuthService _googleAuthService;
    private Controllers.DataBaseServices _dataBaseServices;

    public Login(IGoogleAuthService googleAuthService)
    {
        InitializeComponent();
        _googleAuthService = googleAuthService;
        _dataBaseServices = new Controllers.DataBaseServices();
    }
    private async void OnGoogleLoginClicked(object sender, EventArgs e)
    {
        try
        {
            var loggedUser = await _googleAuthService.GetCurrentUserAsync() ?? await _googleAuthService.AuthenticateAsync();

            if (loggedUser == null) return;

            string idUser = await _dataBaseServices.ObtenerIdPorEmailAsync(loggedUser.Email);

            if (string.IsNullOrEmpty(idUser))
            {
                await _dataBaseServices.InsertarPerfilAsync(loggedUser.Email);
                idUser = await _dataBaseServices.ObtenerIdPorEmailAsync(loggedUser.Email);
            }

            bool esAdmin = await _dataBaseServices.EsUsuarioAdminAsync(idUser);

            string nombreUsuario = !string.IsNullOrWhiteSpace(loggedUser.FullName)
                                    ? loggedUser.FullName
                                    : loggedUser.Email.Split('@')[0];

            // 3. Armamos el perfil en memoria
            var perfil = new UsuarioPerfil
            {
                Id = idUser,
                Email = loggedUser.Email,
                EsAdmin = esAdmin,
                Nombre = nombreUsuario
            };

            // 4. Capturar y guardar el token FCM para Push Notifications
            await GuardarTokenFcmAsync(idUser);

            // 5. Notificación de bienvenida (fire-and-forget)
            _ = NotificationService.NotificarBienvenidaAsync(_dataBaseServices, idUser, nombreUsuario, esAdmin);

            Application.Current.MainPage = new MainNavigation(perfil);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error de Sesión", $"Ocurrió un problema: {ex.Message}", "Ok");
        }
    }

    /// <summary>
    /// Captura el token FCM del dispositivo y lo persiste en Supabase.
    /// Usa la API nativa de Firebase.Messaging directamente (sin plugin de terceros).
    /// También registra un listener para refrescar el token si Firebase lo renueva.
    /// </summary>
    private async Task GuardarTokenFcmAsync(string userId)
    {
        try
        {
#if ANDROID
            // Intentar obtener el token FCM.
            // Primero verificamos si nuestro servicio ya tiene un token cacheado.
            string? fcmToken = PizzeriaApp.Platforms.Android.PizzeriaFirebaseMessagingService.LastKnownToken;

            // Si no hay token cacheado, solicitarlo a Firebase
            if (string.IsNullOrEmpty(fcmToken))
            {
                try
                {
                    var firebaseInstance = Firebase.Messaging.FirebaseMessaging.Instance;
                    var tokenTaskSource = new TaskCompletionSource<string?>();

                    firebaseInstance.GetToken()
                        .AddOnSuccessListener(new PizzeriaApp.Platforms.Android.TokenSuccessListener(tokenTaskSource))
                        .AddOnFailureListener(new PizzeriaApp.Platforms.Android.TokenFailureListener(tokenTaskSource));

                    // Esperar máximo 10 segundos para no bloquear el login
                    var completedTask = await Task.WhenAny(tokenTaskSource.Task, Task.Delay(10000));
                    if (completedTask == tokenTaskSource.Task)
                    {
                        fcmToken = tokenTaskSource.Task.Result;
                    }
                }
                catch (Exception tokenEx)
                {
                    Console.WriteLine($"FCM: Error solicitando token a Firebase: {tokenEx.Message}");
                }
            }

            if (!string.IsNullOrEmpty(fcmToken))
            {
                await _dataBaseServices.GuardarFcmTokenAsync(userId, fcmToken);
                Console.WriteLine($"FCM Token guardado: ...{fcmToken[^6..]}");
            }
            else
            {
                Console.WriteLine("FCM: Token aún no disponible, se guardará cuando Firebase lo genere.");
            }

            // Listener: si Firebase renueva el token, guardarlo automáticamente
            PizzeriaApp.Platforms.Android.PizzeriaFirebaseMessagingService.TokenRefreshed += async (nuevoToken) =>
            {
                if (!string.IsNullOrEmpty(nuevoToken))
                {
                    await _dataBaseServices.GuardarFcmTokenAsync(userId, nuevoToken);
                    Console.WriteLine($"FCM Token renovado y guardado: ...{nuevoToken[^6..]}");
                }
            };
#endif
        }
        catch (Exception ex)
        {
            // No bloquear el login si falla la captura del token FCM
            Console.WriteLine($"FCM: Error capturando token: {ex.Message}");
        }
    }
}