using Android.Gms.Auth.Api.SignIn;
using Android.App;

namespace PizzeriaApp.GoogleAuth
{
    public partial class GoogleAuthService
    {
        public static GoogleSignInOptions _gso;
        public static GoogleSignInClient _googleSignInClient;

        private TaskCompletionSource<GoogleUserDTO> _taskCompletionSource;

        public GoogleAuthService()
        {
            // El constructor ahora es "seguro". Solo suscribe el evento de respuesta.
            // NO tocamos Platform.CurrentActivity aquí porque aún es null.
            MainActivity.ResultGoogleAuth += MainActivity_ResultGoogleAuth;
        }

        // Método auxiliar: Construye el puente a Google solo cuando es seguro hacerlo
        private void AsegurarClienteGoogle()
        {
            if (_googleSignInClient == null)
            {
                _gso = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
                                .RequestIdToken(WebApiKey)
                                .RequestEmail()
                                .RequestId()
                                .RequestProfile()
                                .Build();

                // Aquí garantizamos que Android ya está listo y la Activity existe
                _googleSignInClient = GoogleSignIn.GetClient(Platform.CurrentActivity, _gso);
            }
        }

        public Task<GoogleUserDTO> AuthenticateAsync()
        {
            // Verificamos el cliente antes de usarlo
            AsegurarClienteGoogle();

            _taskCompletionSource = new TaskCompletionSource<GoogleUserDTO>();
            Platform.CurrentActivity.StartActivityForResult(_googleSignInClient.SignInIntent, 9001);

            return _taskCompletionSource.Task;
        }

        public async Task<GoogleUserDTO> GetCurrentUserAsync()
        {
            // Verificamos el cliente antes de usarlo
            AsegurarClienteGoogle();

            try
            {
                var user = await _googleSignInClient.SilentSignInAsync();

                if (user != null)
                {
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
                return null;
            }
        }

        public Task LogoutAsync()
        {
            // Verificamos el cliente antes de usarlo
            AsegurarClienteGoogle();

            if (_googleSignInClient != null)
            {
                _googleSignInClient.SignOut();
            }

            return Task.CompletedTask;
        }

        private void MainActivity_ResultGoogleAuth(object sender, (bool Success, GoogleSignInAccount Account) e)
        {
            if (e.Success)
            {
                try
                {
                    var currentAccount = e.Account;

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
                    _taskCompletionSource?.SetException(ex);
                }
            }
        }
    }
}