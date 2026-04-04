using Microsoft.Maui.Controls;
using PizzeriaApp.Models;
using Microsoft.Extensions.DependencyInjection;

namespace PizzeriaApp.Views
{
    public partial class MainNavigation : FlyoutPage
    {
        public MainNavigation(UsuarioPerfil usuario)
        {
            InitializeComponent();
            ConfigurarMenu(usuario);

            if (usuario.EsAdmin)
            {
                Detail = new NavigationPage(new MenuAdmin());
            }
            else
            {
                Detail = new NavigationPage(new MenuClient(usuario));
            }
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
                var authService = App.Services.GetService<PizzeriaApp.GoogleAuth.IGoogleAuthService>();

                if (authService != null)
                {
                    await authService.LogoutAsync();
                }

                var loginPage = App.Services.GetService<Login>();
                Application.Current.MainPage = new NavigationPage(loginPage);
            };

            MenuContainer.Children.Add(btnCerrar);
        }

        private void CambiarDetalle(Page page)
        {
            Detail = new NavigationPage(page);
            IsPresented = false;
        }
    }
}