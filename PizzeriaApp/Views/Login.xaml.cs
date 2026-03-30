using PizzeriaApp.GoogleAuth;
using PizzeriaApp.Models;
using PizzeriaApp.Data;
using PizzeriaApp.Views; // Asumiendo que MenuAdmin y MenuClient están aquí

namespace PizzeriaApp.Views;

public partial class Login : ContentPage
{
    // 1. Usamos readonly pero NO los instanciamos con "new" aquí.
    private readonly IGoogleAuthService _googleAuthService;
    private readonly DataBaseServices _dataBaseServices;

    // 2. INYECCIÓN DE DEPENDENCIAS: 
    // Al ponerlos en el constructor, .NET MAUI se encarga de ir a buscar la conexión 
    // de Supabase que dejaste en MauiProgram.cs y entregártela lista para usar.
    // ¡Adiós a repetir el link de Supabase!
    public Login(IGoogleAuthService googleAuthService, DataBaseServices dataBaseServices)
    {
        InitializeComponent();
        _googleAuthService = googleAuthService;
        _dataBaseServices = dataBaseServices;
    }

    private async void OnGoogleLoginClicked(object sender, EventArgs e)
    {
        try
        {
            // 3. Obtenemos el usuario de Google
            var loggedUser = await _googleAuthService.GetCurrentUserAsync();
            if (loggedUser == null)
            {
                loggedUser = await _googleAuthService.AuthenticateAsync();
            }

            if (loggedUser == null) return;

            // 4. LÓGICA LINEAL (Adiós al ciclo while)
            // Primero preguntamos: ¿Este correo ya está en mi base de datos?
            string _idUser = await _dataBaseServices.ObtenerIdPorEmailAsync(loggedUser.Email);

            // Si es null, significa que es un cliente nuevo. Lo registramos inmediatamente.
            if (_idUser == null)
            {
                // NOTA: Asumo que loggedUser tiene un .Id (el de Google). Si no, puedes generar uno con Guid.NewGuid().ToString()
                _idUser = Guid.NewGuid().ToString(); // Generamos un ID único

                // CUIDADO: Antes mandabas el FullName, pero el método pide un ID.
                bool exitoAlGuardar = await _dataBaseServices.InsertarPerfilAsync(_idUser, loggedUser.Email);

                if (!exitoAlGuardar)
                {
                    // Si la base de datos falla, abortamos el login para no dejarlo pasar con error.
                    throw new Exception("No se pudo crear tu perfil en la base de datos.");
                }
            }

            // 5. En este punto de la línea de código, ya estamos 100% seguros de que el usuario
            // existe (ya sea porque lo encontramos, o porque lo acabamos de registrar).
            // Ahora solo preguntamos su rol.
            bool _esAdmin = await _dataBaseServices.EsUsuarioAdminAsync(_idUser);

            if (_esAdmin)
            {
                Application.Current.MainPage = new MenuAdmin(_idUser);
            }
            else
            {
                Application.Current.MainPage = new MenuClient(_idUser);
            }
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Login Error", $"Ocurrió un error: {ex.Message}", "Ok");
        }
    }
}