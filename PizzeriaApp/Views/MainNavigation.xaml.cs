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
                Detail = new NavigationPage(new MenuClient(usuario));
            }
            else
            {
                Detail = new NavigationPage(new MenuClient(usuario));
            }
        }

        private void ConfigurarMenu(UsuarioPerfil usuario)
        {
            lblNombreUsuario.Text = $"Hola, {usuario.Nombre ?? "Usuario"}";
            lblRol.Text = usuario.EsAdmin ? "Administrador" : "Cliente Frecuente";
            MenuContainer.Children.Clear();

            if (usuario.EsAdmin)
            {
                AgregarBotonMenu("🏠 Tomar Orden (Mostrador)", new MenuClient(usuario));
                AgregarBotonMenu("👨‍🍳 Cola de Cocina", new ColaCocina());
                AgregarBotonMenu("📦 Gestión de Catálogo", new GestionCatalogo());
                AgregarBotonMenu("📊 Reportes", new ReportesAdmin());
            }
            else
            {
                AgregarBotonMenu("🏠 Nuestro Menú", new MenuClient(usuario));
                AgregarBotonMenu("👤 Mi Perfil", new PerfilCliente(usuario));
                AgregarBotonMenu("📜 Mis Pedidos", new HistorialCliente(usuario.Id));
            }

            AgregarBotonCerrarSesion();
        }

        private void AgregarBotonMenu(string texto, Page paginaDestino)
        {
            var btn = new Button
            {
                Text = texto,
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#424242"),  // Gris obscuro muy elegante
                HorizontalOptions = LayoutOptions.Start,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 5)
            };

            btn.Clicked += (s, e) => CambiarDetalle(paginaDestino);

            MenuContainer.Children.Add(btn);
        }

        private void AgregarBotonCerrarSesion()
        {
            var btnCerrar = new Button
            {
                Text = "🚪 Cerrar Sesión",
                BackgroundColor = Color.FromArgb("#FFEBEE"), // Fondo rojizo suave
                TextColor = Color.FromArgb("#D32F2F"), // Texto rojo alerta
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 8,
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