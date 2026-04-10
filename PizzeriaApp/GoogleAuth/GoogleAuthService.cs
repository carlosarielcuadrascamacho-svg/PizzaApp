namespace PizzeriaApp.GoogleAuth
{
    // Esta clase es parcial porque el "músculo" de la autenticación vive en la carpeta de Platforms (Android/iOS)
    public partial class GoogleAuthService : IGoogleAuthService
    {
        // Esta es la llave del cliente web que registramos en Google Cloud Console. 
        // Sin esto, Firebase no nos dejaría validar que el usuario es quien dice ser al loguearse para pedir su pizza.
        private const string WebApiKey = "555633638264-ef1vfi2vuhd852hb0e8in4i9flifd62f.apps.googleusercontent.com";
    }
}
