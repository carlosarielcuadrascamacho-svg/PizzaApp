using Microsoft.Maui.Controls;
using PizzeriaApp.Models; // Asegúrate de que este namespace coincida con tu modelo

namespace PizzeriaApp.Views
{
    public partial class MainNavigation : FlyoutPage
    {
        // Obligamos a que la página reciba el perfil del usuario validado
        public MainNavigation(UsuarioPerfil usuario)
        {
            InitializeComponent();
            ConfigurarMenu(usuario);
        }

        // Fragmento a modificar en Views/MainNavigation.xaml.cs
        private void ConfigurarMenu(UsuarioPerfil usuario)
        {
            // Limpiamos el contenedor por seguridad
            MenuContainer.Children.Clear();

            // Ahora evaluamos tu propiedad booleana
            if (usuario.EsAdmin)
            {
                AgregarBotonMenu("Ver Pedidos", new MenuAdmin(usuario.Id));
                // AgregarBotonMenu("Reportes", new ReportesAdmin());
            }
            else
            {
                AgregarBotonMenu("Realizar Pedido", new MenuClient(usuario.Id));
                // AgregarBotonMenu("Mi Historial", new HistorialClient());
            }

            AgregarBotonCerrarSesion();
        }

        // Método auxiliar para crear botones limpios y asignarles el evento de navegación
        private void AgregarBotonMenu(string texto, Page paginaDestino)
        {
            var btn = new Button
            {
                Text = texto,
                BackgroundColor = Colors.Transparent,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Start,
                FontSize = 16,
                Margin = new Thickness(0, 5)
            };

            // Al hacer clic, ejecuta el método CambiarDetalle
            btn.Clicked += (s, e) => CambiarDetalle(paginaDestino);

            MenuContainer.Children.Add(btn);
        }

        private void AgregarBotonCerrarSesion()
        {
            var btnCerrar = new Button
            {
                Text = "Cerrar Sesión",
                BackgroundColor = Colors.DarkRed,
                TextColor = Colors.White,
                Margin = new Thickness(0, 30, 0, 0) // Separarlo del resto del menú
            };

            // En Views/MainNavigation.xaml.cs, dentro de tu método AgregarBotonCerrarSesion()
            btnCerrar.Clicked += async (s, e) =>
            {
                // Instanciamos el servicio de auth
                var authService = new PizzeriaApp.GoogleAuth.GoogleAuthService();

                // Le pedimos a Android que olvide la cuenta
                await authService.LogoutAsync();

                // Redirigimos al inicio
                Application.Current.MainPage = new NavigationPage(new Login());
            };

            MenuContainer.Children.Add(btnCerrar);
        }

        // Método centralizado para cambiar la vista principal
        private void CambiarDetalle(Page page)
        {
            Detail = new NavigationPage(page);
            IsPresented = false; // Esto es crucial: oculta el menú lateral tras hacer clic
        }
    }
}