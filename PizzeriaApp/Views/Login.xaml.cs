using PizzeriaApp.Models;
using PizzeriaApp.GoogleAuth;
using PizzeriaApp.Views;

namespace PizzeriaApp.Views;

public partial class Login : ContentPage
{
    // 1. Quitamos los "new ...()". Solo dejamos las declaraciones limpias.
    private IGoogleAuthService _googleAuthService;
    private Controllers.DataBaseServices _dataBaseServices;

    public Login()
    {
        InitializeComponent();
    }

    private async void OnGoogleLoginClicked(object sender, EventArgs e)
    {
        try
        {
            // 2. LAZY INITIALIZATION: 
            // Como el usuario ya pudo ver el botón y hacerle clic, te aseguro al 100% 
            // que la Activity de Android ya existe y no será nula.
            if (_googleAuthService == null)
            {
                _googleAuthService = new GoogleAuthService();
            }

            if (_dataBaseServices == null)
            {
                // Reemplaza "URL" y "KEY" por tus credenciales de Supabase
                _dataBaseServices = new Controllers.DataBaseServices(new Supabase.Client("URL", "KEY"));
            }

            // 3. Autenticación (El resto de tu código se mantiene intacto)
            var loggedUser = await _googleAuthService.GetCurrentUserAsync() ?? await _googleAuthService.AuthenticateAsync();

            if (loggedUser == null) return;

            string idUser = await _dataBaseServices.ObtenerIdPorEmailAsync(loggedUser.Email);

            if (string.IsNullOrEmpty(idUser))
            {
                string nombreProvisional = loggedUser.Email.Split('@')[0];
                await _dataBaseServices.InsertarPerfilAsync(nombreProvisional, loggedUser.Email);
                idUser = await _dataBaseServices.ObtenerIdPorEmailAsync(loggedUser.Email);
            }

            bool esAdmin = await _dataBaseServices.EsUsuarioAdminAsync(idUser);

            var perfil = new UsuarioPerfil
            {
                Id = idUser,
                Email = loggedUser.Email,
                EsAdmin = esAdmin
            };

            Application.Current.MainPage = new MainNavigation(perfil);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error de Sesión", $"Ocurrió un problema: {ex.Message}", "Ok");
        }
    }
}