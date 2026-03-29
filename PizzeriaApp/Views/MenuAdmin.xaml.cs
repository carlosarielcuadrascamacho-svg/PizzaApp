using Microsoft.Extensions.DependencyInjection;
using PizzeriaApp.GoogleAuth;

namespace PizzeriaApp.Views;

public partial class MenuAdmin : FlyoutPage
{
    private readonly IGoogleAuthService _googleAuthService = new GoogleAuthService();
    private readonly string UsuarioId;
	public MenuAdmin(string usuarioId) //Constructor de la clase MenuAdmin, recibe el id del usuario como parametro para mostrar su perfil en la pantalla del admin
    {
		InitializeComponent();
        lblIdFlyout.Text = usuarioId;
        lblIdDetail.Text = usuarioId;
        UsuarioId = usuarioId;
    }
    protected override async void OnAppearing() //Metodo que se ejecuta cuando la pantalla del admin aparece, muestra un mensaje de confirmacion con el id del usuario recibido como parametro (para verificar que se ha recibido correctamente el id del usuario y se ha redirigido a la pantalla del admin)
    {
        base.OnAppearing();
        if (!string.IsNullOrEmpty(UsuarioId))
        {
            // Mostramos el mensaje de confirmación
            await DisplayAlert("Conexión Exitosa", $"ID de usuario recibido: {UsuarioId}", "Aceptar");
        }
    }

    private async void Logout_Clicked(object sender, EventArgs e) //Metodo que se ejecuta cuando el usuario hace clic en el boton de logout, cierra la sesion del usuario y redirige a la pantalla de login
    {
        await _googleAuthService?.LogoutAsync(); // Cierra la sesión del usuario utilizando el servicio de autenticación de Google

        await Application.Current.MainPage.DisplayAlert("Login Message", "Goodbye", "Ok");

        // Resolve Login from the DI container so its constructor dependencies are provided
        var loginPage = App.Services.GetRequiredService<Login>();
        Application.Current.MainPage = loginPage;
    }
}