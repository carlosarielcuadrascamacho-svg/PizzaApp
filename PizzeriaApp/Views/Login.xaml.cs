
using PizzeriaApp.Controllers;
using PizzeriaApp.GoogleAuth;
using Supabase;
using Supabase.Gotrue;
using static Supabase.Gotrue.Constants;
using PizzeriaApp.Models;
namespace PizzeriaApp.Views;


public partial class Login : ContentPage
{

    private readonly IGoogleAuthService _googleAuthService = new GoogleAuthService(); //Instancia del servicio de autenticacion de Google para manejar el proceso de login
    private readonly DataBaseServices _dataBaseServices = new DataBaseServices(new Supabase.Client("https://aggsgpvobhnrbpwxhdor.supabase.co", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImFnZ3NncHZvYmhucmJwd3hoZG9yIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzQ0NDUxMzYsImV4cCI6MjA5MDAyMTEzNn0.vl5wlweNtIZF01FQlZnb_zgRqEvtAu7IrgbAiV7q8Aw"));//Instancia del servicio de base de datos para manejar las operaciones relacionadas con los perfiles de usuario en la base de datos Supabase


    public Login()
	{
		InitializeComponent();
    }

    private async void OnGoogleLoginClicked(object sender, EventArgs e)
    {
        try
        {
            
            var loggedUser = await _googleAuthService.GetCurrentUserAsync();//Intentar obtener el usuario actual, si no hay ninguno se inicia el proceso de autenticacion
            bool key = true;

            if (loggedUser == null)
            {
                loggedUser = await _googleAuthService.AuthenticateAsync();
            }
            //Apartado de verificacion de usuario en la base de datos, si no existe se crea un nuevo perfil, si existe se redirige a la pantalla correspondiente segun el rol del usuario

            while (key) //La neta este while no me convence del todo pero fue la forma que pensé de ahorrar codigo repetido, si conoces una mejor forma de hacerlo sin repetir codigo adelante
            {
                string _idUser = await _dataBaseServices.ObtenerIdPorEmailAsync(loggedUser.Email); //Metodo para obtener el id por medio del Email
                if (_idUser != null)
                {
                    bool _esAdmin = await _dataBaseServices.EsUsuarioAdminAsync(_idUser);//Metodo para verificar si el usuario es admin o cliente

                    if (_esAdmin)//Verificar el rol del usuario y redirigir a la pantalla correspondiente
                    {
                        var parametros = new Dictionary<string, object>//Se crea un diccionario para enviar los parametros necesarios a la pantalla del admin, en este caso el id del usuario para mostrar su perfil
                    {
                        {"UsuarioId", _idUser }
                    };
                        key = false;
                        Application.Current.MainPage = new MenuAdmin(_idUser);//Redirigir a la pantalla del admin, pasando el id del usuario como parametro
                    }
                    else
                    {
                        var parametros = new Dictionary<string, object>//Se crea un diccionario para enviar los parametros necesarios a la pantalla del cliente, en este caso el id del usuario para mostrar su perfil
                    {
                        {"UsuarioId", _idUser }
                    };
                        key = false;
                        
                        Application.Current.MainPage = new MenuClient(_idUser);//Redirigir a la pantalla del cliente, pasando el id del usuario como parametro


                    }
                }
                else
                {
                    await _dataBaseServices.InsertarPerfilAsync(loggedUser.FullName, loggedUser.Email);//Si el usuario no existe en la base de datos, se crea un nuevo perfil con el nombre completo y el email del usuario autenticado
                }
            }
        }
        catch(Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Login Error", $"An error occurred: {ex.Message}", "Ok");
            return;
        }
        

    }
}


