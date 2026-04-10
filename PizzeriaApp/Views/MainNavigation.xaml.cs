using System;
using System.IO;
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

        private async void ConfigurarMenu(UsuarioPerfil usuario)
        {
            // Cargar foto de perfil y nombre dinámicamente
            if (usuario.EsAdmin)
            {
                // Admin: siempre pizzasteve y nombre genérico
                lblNombreUsuario.Text = "Administrador";
                lblRol.Text = "Panel de Control";
                imgFlyoutPerfil.Source = "pizzasteve.png";
            }
            else
            {
                // Cliente: cargar datos frescos desde la BD
                lblRol.Text = "Cliente Frecuente";
                lblNombreUsuario.Text = $"Hola, {usuario.Nombre ?? "Usuario"}";
                imgFlyoutPerfil.Source = "pizzasteve.png"; // Default mientras carga

                try
                {
                    var dbService = new PizzeriaApp.Controllers.DataBaseServices();
                    var perfilFresco = await dbService.ObtenerPerfilAsync(usuario.Id);

                    if (perfilFresco != null)
                    {
                        // Actualizar nombre
                        lblNombreUsuario.Text = $"Hola, {perfilFresco.Nombre ?? "Usuario"}";

                        // Actualizar foto de perfil
                        if (!string.IsNullOrEmpty(perfilFresco.FotoPerfil))
                        {
                            var fotoBase64 = perfilFresco.FotoPerfil;
                            if (fotoBase64.Contains(","))
                                fotoBase64 = fotoBase64.Substring(fotoBase64.IndexOf(",") + 1);

                            byte[] imageBytes = Convert.FromBase64String(fotoBase64);
                            imgFlyoutPerfil.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error cargando perfil en flyout: {ex.Message}");
                }
            }
            
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
                AgregarBotonMenu("📜 Mis Pedidos", new HistorialCliente(usuario.Id, usuario.Nombre ?? "Cliente"));
                AgregarBotonMenu("👤 Mi Perfil", new PerfilCliente(usuario));
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