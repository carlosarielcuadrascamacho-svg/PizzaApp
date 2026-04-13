using System;
using System.IO;
using Microsoft.Maui.Controls;
using PizzeriaApp.Models;
using Microsoft.Extensions.DependencyInjection;

namespace PizzeriaApp.Views
{
    // Esta es la estructura principal de navegación (FlyoutPage), el "esqueleto" que tiene el menú lateral y el contenido central
    public partial class MainNavigation : FlyoutPage
    {
        public MainNavigation(UsuarioPerfil usuario)
        {
            InitializeComponent();
            // Configuramos qué botones y qué info va a salir en el menú lateral
            ConfigurarMenu(usuario);

            // Por defecto, mostramos el menú de clientes al entrar, sin importar el rol
            // (Los admins también pueden tomar pedidos como si fueran clientes)
            Detail = new NavigationPage(new MenuClient(usuario));
        }

        // Aquí armamos el menú lateral dependiendo de quién se haya logueado
        private async void ConfigurarMenu(UsuarioPerfil usuario)
        {
            // Lógica para personalizar el "Header" del menú (foto y nombre)
            if (usuario.EsAdmin)
            {
                // Si es Admin, ponemos datos genéricos de oficina
                lblNombreUsuario.Text = "Administrador";
                lblRol.Text = "Panel de Control";
                imgFlyoutPerfil.Source = "pizzasteve.png"; // Imagen de marca
            }
            else
            {
                // Si es Cliente, intentamos jalar sus datos frescos de la BD para que se sienta en casa
                lblRol.Text = "Cliente Frecuente";
                lblNombreUsuario.Text = $"Hola, {usuario.Nombre ?? "Usuario"}";
                imgFlyoutPerfil.Source = "pizzasteve.png"; // Imagen default mientras carga la suya

                try
                {
                    // Delegamos la obtención del perfil al AuthController para cumplir con MVC
                    var authController = new Controllers.AuthController(null, new Services.DataBaseServices());
                    var perfilFresco = await authController.ObtenerPerfilAsync(usuario.Id);

                    if (perfilFresco != null)
                    {
                        lblNombreUsuario.Text = $"Hola, {perfilFresco.Nombre ?? "Usuario"}";

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
                    // Si falla el perfil, dejamos los datos default y seguimos; no queremos romper la navegación
                    Console.WriteLine($"Error cargando perfil en flyout: {ex.Message}");
                }
            }
            
            // Limpiamos los botones viejos (por si hubo un cambio de sesión sin cerrar la app)
            MenuContainer.Children.Clear();

            // Inyectamos los botones según el rol con iconos
            if (usuario.EsAdmin)
            {
                AgregarSeccion("OPERACIONES");
                AgregarItemMenu("🏠", "Tomar Orden", new MenuClient(usuario));
                AgregarItemMenu("👨‍🍳", "Cola de Cocina", new ColaCocina());
                
                AgregarSeccion("ADMINISTRACIÓN");
                AgregarItemMenu("📦", "Gestión de Catálogo", new GestionCatalogo());
                AgregarItemMenu("📊", "Reportes Directivos", new ReportesAdmin());
            }
            else
            {
                AgregarSeccion("PRINCIPAL");
                AgregarItemMenu("🏠", "Nuestro Menú", new MenuClient(usuario));
                AgregarItemMenu("📜", "Mis Pedidos", new HistorialCliente(usuario.Id, usuario.Nombre ?? "Cliente"));
                
                AgregarSeccion("PERSONAL");
                AgregarItemMenu("👤", "Mi Perfil", new PerfilCliente(usuario));
            }

            AgregarBotonCerrarSesion();
        }

        private void AgregarSeccion(string titulo)
        {
            MenuContainer.Children.Add(new Label 
            { 
                Text = titulo, 
                FontSize = 10, 
                FontAttributes = FontAttributes.Bold, 
                TextColor = Color.FromArgb("#BDBDBD"),
                Margin = new Thickness(10, 20, 0, 5),
                CharacterSpacing = 1
            });
        }

        private void AgregarItemMenu(string icono, string texto, Page paginaDestino)
        {
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition { Width = 40 }, new ColumnDefinition { Width = GridLength.Star } }, Padding = new Thickness(15, 12) };
            
            var lblIcono = new Label { Text = icono, FontSize = 18, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
            var lblTexto = new Label { Text = texto, FontSize = 15, FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center, TextColor = Color.FromArgb("#424242") };

            grid.Add(lblIcono, 0);
            grid.Add(lblTexto, 1);

            var border = new Border
            {
                StrokeThickness = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                BackgroundColor = Colors.Transparent,
                Content = grid,
                Margin = new Thickness(0, 2)
            };

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) => {
                // Limpiar selecciones previas
                foreach (var child in MenuContainer.Children)
                {
                    if (child is Border b) b.BackgroundColor = Colors.Transparent;
                }
                // Resaltar actual
                border.BackgroundColor = Color.FromArgb("#F5F5F5");
                CambiarDetalle(paginaDestino);
            };

            border.GestureRecognizers.Add(tapGesture);
            MenuContainer.Children.Add(border);
        }

        private void AgregarBotonCerrarSesion()
        {
            var btnCerrar = new Border
            {
                StrokeThickness = 1,
                Stroke = Color.FromArgb("#FFCDD2"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                BackgroundColor = Color.FromArgb("#FFF5F5"),
                Margin = new Thickness(0, 40, 0, 0),
                Padding = new Thickness(15, 12),
                Content = new Label 
                { 
                    Text = "🚪 Cerrar Sesión", 
                    TextColor = Color.FromArgb("#D32F2F"), 
                    FontAttributes = FontAttributes.Bold, 
                    HorizontalOptions = LayoutOptions.Center 
                }
            };

            var tap = new TapGestureRecognizer();
            tap.Tapped += async (s, e) =>
            {
                var authService = App.Services.GetService<PizzeriaApp.GoogleAuth.IGoogleAuthService>();
                if (authService != null) await authService.LogoutAsync();

                var loginPage = App.Services.GetService<Login>();
                Application.Current.MainPage = new NavigationPage(loginPage);
            };

            btnCerrar.GestureRecognizers.Add(tap);
            MenuContainer.Children.Add(btnCerrar);
        }

        // Cambia la vista central y cierra automáticamente el menú lateral para comodidad del usuario
        private void CambiarDetalle(Page page)
        {
            Detail = new NavigationPage(page);
            IsPresented = false; // Esto es lo que "esconde" el menú lateral
        }
    }
}