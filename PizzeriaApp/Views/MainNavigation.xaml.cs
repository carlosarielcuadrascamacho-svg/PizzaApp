using Microsoft.Maui.Controls;
using PizzeriaApp.Models;
using Microsoft.Extensions.DependencyInjection; // Para App.Services

namespace PizzeriaApp.Views
{
    public partial class MainNavigation : FlyoutPage
    {
        // 1. OBLIGATORIO: Recibir el usuario validado
        public MainNavigation(UsuarioPerfil usuario)
        {
            InitializeComponent();
            ConfigurarMenu(usuario);
        }

        private void ConfigurarMenu(UsuarioPerfil usuario)
        {
            lblNombreUsuario.Text = $"Hola, {usuario.Nombre ?? "Usuario"}";
            MenuContainer.Children.Clear();

            if (usuario.EsAdmin)
            {
                AgregarBotonMenu("Ver Pedidos", new MenuAdmin());
            }
            else
            {
                // AQUÍ: Le pasamos la información del usuario en memoria a la vista del cliente
                AgregarBotonMenu("Realizar Pedido", new MenuClient(usuario));
            }

            AgregarBotonCerrarSesion();
        }

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

            // 3. El evento click cambia la vista "Detail"
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
                Margin = new Thickness(0, 30, 0, 0)
            };

            btnCerrar.Clicked += async (s, e) =>
            {
                // Obtenemos el servicio Singleton
                var authService = App.Services.GetService<PizzeriaApp.GoogleAuth.IGoogleAuthService>();

                if (authService != null)
                {
                    // Llamamos al método nativo que borra la sesión en Android
                    await authService.LogoutAsync();
                }

                // Destruimos la navegación y volvemos al Login
                var loginPage = App.Services.GetService<Login>();
                Application.Current.MainPage = new NavigationPage(loginPage);
            };

            MenuContainer.Children.Add(btnCerrar);
        }

        // 4. Método vital para intercambiar la pantalla
        private void CambiarDetalle(Page page)
        {
            // Asigna la nueva página envuelta en navegación a la zona derecha
            Detail = new NavigationPage(page);
            // Oculta el menú lateral automáticamente
            IsPresented = false;
        }
    }
}