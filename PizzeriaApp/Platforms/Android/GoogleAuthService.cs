using Android.Gms.Auth.Api.SignIn;
using Android.App;

namespace PizzeriaApp.GoogleAuth
{
    // Aquí es donde realmente sucede la magia en Android para que el cliente se loguee con Google
    public partial class GoogleAuthService
    {
        // Guardamos las opciones y el cliente de forma estática para que persistan mientras la app esté viva
        public static GoogleSignInOptions _gso;
        public static GoogleSignInClient _googleSignInClient;

        // Este objeto es el que nos permite esperar "asíncronamente" a que el usuario termine de interactuar con el modal de Google
        private TaskCompletionSource<GoogleUserDTO> _taskCompletionSource;

        public GoogleAuthService()
        {
            // El constructor ahora es "seguro". Solo nos suscribimos al evento que lanzará la MainActivity cuando Google responda.
            // Es vital no tocar nada de la UI aquí porque el Activity podría no estar listo todavía.
            MainActivity.ResultGoogleAuth += MainActivity_ResultGoogleAuth;
        }

        // Este método es nuestro seguro de vida: se asegura de que el cliente de Google esté listo antes de usarlo
        private void AsegurarClienteGoogle()
        {
            if (_googleSignInClient == null)
            {
                // Configuramos qué datos le vamos a pedir a Google (Token, Email, Perfil, etc.)
                _gso = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
                                .RequestIdToken(WebApiKey) // Usamos la WebApiKey que definimos en la clase compartida
                                .RequestEmail()
                                .RequestId()
                                .RequestProfile()
                                .Build();

                // Aquí ya podemos pedir el cliente porque garantizamos que estamos en el hilo de ejecución correcto y con la Activity lista
                _googleSignInClient = GoogleSignIn.GetClient(Platform.CurrentActivity, _gso);
            }
        }

        // Este es el método que llamamos desde el ViewModel cuando el usuario pica el botón de "Entrar con Google"
        public Task<GoogleUserDTO> AuthenticateAsync()
        {
            // Primero que nada, verificamos que el puente con Google esté construido
            AsegurarClienteGoogle();

            // Inicializamos la promesa que devolveremos al código compartido
            _taskCompletionSource = new TaskCompletionSource<GoogleUserDTO>();

            // Lanzamos el intent nativo de Android. El código 9001 es solo un ID para identificar esta petición al volver.
            Platform.CurrentActivity.StartActivityForResult(_googleSignInClient.SignInIntent, 9001);

            // Regresamos la tarea. Esta se quedará "colgada" hasta que el usuario elija su cuenta.
            return _taskCompletionSource.Task;
        }

        // Intenta recuperar la sesión si el usuario ya se había logueado antes, así no tiene que elegir cuenta cada que abre la app
        public async Task<GoogleUserDTO> GetCurrentUserAsync()
        {
            AsegurarClienteGoogle();

            try
            {
                // Intentamos un login silencioso; si Google tiene el token fresco, nos lo da de inmediato
                var user = await _googleSignInClient.SilentSignInAsync();

                if (user != null)
                {
                    // Si hubo éxito, mapeamos los datos de Android a nuestro objeto DTO de la Pizzeria
                    return new GoogleUserDTO
                    {
                        Email = user.Email,
                        FullName = $"{user.DisplayName}",
                        TokenId = user.IdToken,
                        UserName = user.GivenName
                    };
                }
                return null;
            }
            catch (Exception)
            {
                // Si algo falla en el login silencioso, simplemente devolvemos nulo para que el usuario haga login manual
                return null;
            }
        }

        // Para cuando el cliente quiera cerrar su sesión y salir de la app de pizzas
        public Task LogoutAsync()
        {
            AsegurarClienteGoogle();

            if (_googleSignInClient != null)
            {
                // Le decimos al cliente de Google que borre los tokens locales
                _googleSignInClient.SignOut();
            }

            return Task.CompletedTask;
        }

        // Este es el manejador del evento que viene desde MainActivity.OnActivityResult
        private void MainActivity_ResultGoogleAuth(object sender, (bool Success, GoogleSignInAccount Account) e)
        {
            if (e.Success)
            {
                try
                {
                    // Si el login fue exitoso, sacamos la cuenta que nos pasó el evento
                    var currentAccount = e.Account;

                    // Llenamos nuestra promesa de C# con los datos que nos entregó Google. 
                    // Esto "despierta" el await que esté esperando en el AuthenticateAsync.
                    _taskCompletionSource?.SetResult(
                        new GoogleUserDTO
                        {
                            Email = currentAccount.Email,
                            FullName = currentAccount.DisplayName,
                            TokenId = currentAccount.IdToken,
                            UserName = currentAccount.GivenName,
                        });
                }
                catch (Exception ex)
                {
                    // Si algo tronó mapeando los datos, mandamos la excepción a la tarea
                    _taskCompletionSource?.SetException(ex);
                }
            }
            // Nota: Podríamos manejar el "e.Success == false" aquí también, por ejemplo si el usuario canceló el diálogo.
        }
    }
}