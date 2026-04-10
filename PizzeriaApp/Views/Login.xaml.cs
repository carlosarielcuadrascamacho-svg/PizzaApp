using PizzeriaApp.Models;
using PizzeriaApp.GoogleAuth;
using PizzeriaApp.Services;
using PizzeriaApp.Views;

namespace PizzeriaApp.Views;

// Esta es la puerta de entrada a la app; aquí manejamos el login con Google y la vinculación con Firebase
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

    // El evento principal: cuando el usuario le pica al botón colorido de Google
    private async void OnGoogleLoginClicked(object sender, EventArgs e)
    {
        try
        {
            // Primero intentamos recuperar una sesión activa o disparamos el modal de login de Google
            var loggedUser = await _googleAuthService.GetCurrentUserAsync() ?? await _googleAuthService.AuthenticateAsync();

            // Si se arrepintió y cerró el modal, no hacemos nada
            if (loggedUser == null) return;

            // Buscamos si este correo ya existe en nuestra base de datos de perfiles
            string idUser = await _dataBaseServices.ObtenerIdPorEmailAsync(loggedUser.Email);

            // Si es la primera vez que entra a la app, le creamos su perfil automáticamente
            if (string.IsNullOrEmpty(idUser))
            {
                await _dataBaseServices.InsertarPerfilAsync(loggedUser.Email);
                // Volvemos a consultar para obtener el UUID generado por Supabase
                idUser = await _dataBaseServices.ObtenerIdPorEmailAsync(loggedUser.Email);
            }

            // Revisamos en una tabla de seguridad si este usuario tiene permisos de Administrador (para la cocina/catálogo)
            bool esAdmin = await _dataBaseServices.EsUsuarioAdminAsync(idUser);

            // Si Google no nos dio nombre, usamos lo que esté antes del '@' del correo como apodo
            string nombreUsuario = !string.IsNullOrWhiteSpace(loggedUser.FullName)
                                    ? loggedUser.FullName
                                    : loggedUser.Email.Split('@')[0];

            // Armamos el objeto de perfil que va a viajar por toda la app durante esta sesión
            var perfil = new UsuarioPerfil
            {
                Id = idUser,
                Email = loggedUser.Email,
                EsAdmin = esAdmin,
                Nombre = nombreUsuario
            };

            // Paso vital: Capturamos el token del dispositivo para poder mandarle Pushes después
            await GuardarTokenFcmAsync(idUser);

            // Mandamos una notificación de bienvenida (proceso en segundo plano para no retrasar la entrada)
            _ = NotificationService.NotificarBienvenidaAsync(_dataBaseServices, idUser, nombreUsuario, esAdmin);

            // Cambiamos la página principal de la app a la navegación con el perfil ya cargado
            Application.Current.MainPage = new MainNavigation(perfil);
        }
        catch (Exception ex)
        {
            // Si algo truena en el handshake con Google o la BD, avisamos al usuario
            await DisplayAlert("Error de Sesión", $"Ocurrió un problema al intentar entrar: {ex.Message}", "Ok");
        }
    }

    // Método para capturar el token FCM y guardarlo en Supabase; esto nos permite enviar notificaciones personalizadas
    private async Task GuardarTokenFcmAsync(string userId)
    {
        try
        {
#if ANDROID
            // Checamos si ya capturamos un token anteriormente en esta ejecución
            string? fcmToken = PizzeriaApp.Platforms.Android.PizzeriaFirebaseMessagingService.LastKnownToken;

            // Si no hay token en caché, se lo pedimos formalmente a Google Play Services
            if (string.IsNullOrEmpty(fcmToken))
            {
                try
                {
                    var firebaseInstance = Firebase.Messaging.FirebaseMessaging.Instance;
                    var tokenTaskSource = new TaskCompletionSource<string?>();

                    // Usamos listeners para convertir las callbacks de Java en una Task de .NET
                    firebaseInstance.GetToken()
                        .AddOnSuccessListener(new PizzeriaApp.Platforms.Android.TokenSuccessListener(tokenTaskSource))
                        .AddOnFailureListener(new PizzeriaApp.Platforms.Android.TokenFailureListener(tokenTaskSource));

                    // Ponemos un timeout de 10 segundos para que el usuario no se quede trabado en el login si Firebase tarda
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

            // Si logramos obtener el token, lo persistimos vinculado al ID del usuario
            if (!string.IsNullOrEmpty(fcmToken))
            {
                await _dataBaseServices.GuardarFcmTokenAsync(userId, fcmToken);
                Console.WriteLine($"FCM Token guardado: ...{fcmToken[^6..]}");
            }
            else
            {
                Console.WriteLine("FCM: Token aún no disponible; se guardará cuando Firebase lo genere.");
            }

            // Dejamos un evento escuchando por si el token cambia mientras la app está abierta (refresco de token)
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
            // Error silencioso: si las notificaciones fallan, no queremos que el usuario no pueda usar la app
            Console.WriteLine($"FCM: Error capturando token: {ex.Message}");
        }
    }
}