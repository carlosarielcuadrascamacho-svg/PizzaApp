using PizzeriaApp.GoogleAuth;

namespace PizzeriaApp.Views;


public partial class MenuClient : FlyoutPage
{
    private readonly IGoogleAuthService _googleAuthService = new GoogleAuthService();

    private readonly string UsuarioId;
    public MenuClient() //Constructor de la clase MenuClient, recibe el id del usuario como parametro para mostrar su perfil en la pantalla del cliente
    {
		InitializeComponent();
    }

    protected override async void OnAppearing() //Metodo que se ejecuta cuando la pantalla del cliente aparece, muestra un mensaje de confirmacion con el id del usuario recibido como parametro (para verificar que se ha recibido correctamente el id del usuario y se ha redirigido a la pantalla del cliente)
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
        await _googleAuthService?.LogoutAsync();

        await Application.Current.MainPage.DisplayAlert("Login Message", "Goodbye", "Ok");

        Application.Current.MainPage = new Login(_googleAuthService);
    }
}