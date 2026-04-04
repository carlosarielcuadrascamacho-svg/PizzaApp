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

            // 1. Resolvemos el nombre AQUÍ ARRIBA para que exista en toda la función.
            // Si FullName tiene algo, lo usamos. Si está vacío, cortamos el correo.
            string nombreUsuario = !string.IsNullOrWhiteSpace(loggedUser.FullName)
                                    ? loggedUser.FullName
                                    : loggedUser.Email.Split('@')[0];

            string idUser = await _dataBaseServices.ObtenerIdPorEmailAsync(loggedUser.Email);

            // 2. Si el usuario no existe, lo insertamos
            if (string.IsNullOrEmpty(idUser))
            {
                // Usamos la variable que ya resolvimos arriba
                await _dataBaseServices.InsertarPerfilAsync(nombreUsuario, loggedUser.Email);
                idUser = await _dataBaseServices.ObtenerIdPorEmailAsync(loggedUser.Email);
            }

            // 3. Validamos si es administrador
            bool esAdmin = await _dataBaseServices.EsUsuarioAdminAsync(idUser);

            // 4. Construimos el perfil final
            var perfil = new UsuarioPerfil
            {
                Id = idUser,
                Email = loggedUser.Email,
                Nombre = nombreUsuario, // Usamos la variable global de esta función
                EsAdmin = esAdmin
            };

            // 5. Navegamos pasando el perfil correctamente
            Application.Current.MainPage = new MainNavigation(perfil);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error de Sesión", $"Ocurrió un problema: {ex.Message}", "Ok");
        }
    }
}