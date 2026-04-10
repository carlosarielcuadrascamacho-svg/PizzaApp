using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Auth.Api.SignIn;
using Android.OS;

namespace PizzeriaApp
{
    // Esta es la entrada principal de nuestra app en Android; aquí se configura el tema y cómo se lanza la actividad
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        // Definimos este evento estático para poder avisarle al GoogleAuthService cuando el usuario termine de elegir su cuenta
        public static event EventHandler<(bool Success, GoogleSignInAccount account)> ResultGoogleAuth;

        // Este método es el que Android llama cuando una actividad externa (como el selector de cuentas de Google) termina y regresa a nuestra app
        protected override async void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            // Siempre llamamos a la base para que MAUI siga funcionando correctamente por debajo
            base.OnActivityResult(requestCode, resultCode, data);

            // Filtramos por el código 9001, que es el que definimos nosotros para el login de Google
            if (requestCode == 9001)
            {
                try
                {
                    // Intentamos obtener la cuenta de la respuesta que nos dio Android
                    var currentAccount = await GoogleSignIn.GetSignedInAccountFromIntentAsync(data);

                    // Si todo salió bien, disparamos el evento para que el servicio de autenticación reciba los datos del cliente
                    ResultGoogleAuth?.Invoke(this, (currentAccount.Email != null, currentAccount));
                }
                catch (Exception ex)
                {
                    // Si el usuario canceló o hubo un error de red, notificamos el fallo para no dejar la app colgada
                    ResultGoogleAuth?.Invoke(this, (false, null));
                }
            }
        }
    }
}
