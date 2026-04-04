using PizzeriaApp.Models;
using PizzeriaApp.GoogleAuth;
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

            Application.Current.MainPage = new MainNavigation(perfil);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error de Sesión", $"Ocurrió un problema: {ex.Message}", "Ok");
        }
    }
}