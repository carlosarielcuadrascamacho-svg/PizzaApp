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
                    // Consultamos Supabase para ver si tiene una foto de perfil personalizada
                    var dbService = new PizzeriaApp.Controllers.DataBaseServices();
                    var perfilFresco = await dbService.ObtenerPerfilAsync(usuario.Id);

                    if (perfilFresco != null)
                    {
                        lblNombreUsuario.Text = $"Hola, {perfilFresco.Nombre ?? "Usuario"}";

                        // Si tiene foto en base64, la convertimos para mostrarla en el menú
                        if (!string.IsNullOrEmpty(perfilFresco.FotoPerfil))
                        {
                            var fotoBase64 = perfilFresco.FotoPerfil;
                            // Limpiamos el prefijo si es que viene con el formato de data:image/png;base64,...
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

            // Inyectamos los botones según el rol
            if (usuario.EsAdmin)
            {
                // El Admin tiene acceso a todo el control operativo
                AgregarBotonMenu("🏠 Tomar Orden (Mostrador)", new MenuClient(usuario));
                AgregarBotonMenu("👨‍🍳 Cola de Cocina", new ColaCocina());
                AgregarBotonMenu("📦 Gestión de Catálogo", new GestionCatalogo());
                AgregarBotonMenu("📊 Reportes", new ReportesAdmin());
            }
            else
            {
                // El Cliente solo ve lo que le interesa: comer y sus datos
                AgregarBotonMenu("🏠 Nuestro Menú", new MenuClient(usuario));
                AgregarBotonMenu("📜 Mis Pedidos", new HistorialCliente(usuario.Id, usuario.Nombre ?? "Cliente"));
                AgregarBotonMenu("👤 Mi Perfil", new PerfilCliente(usuario));
            }

            // El botón de salida siempre va al final
            AgregarBotonCerrarSesion();
        }

        // Método auxiliar para crear botones de menú con estilo uniforme
        private void AgregarBotonMenu(string texto, Page paginaDestino)
        {
            var btn = new Button
            {
                Text = texto,
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#424242"), // Un gris oscuro elegante para el texto
                HorizontalOptions = LayoutOptions.Start,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 5)
            };

            // Al picar el botón, cambiamos el "Detail" (la página central) por la nueva
            btn.Clicked += (s, e) => CambiarDetalle(paginaDestino);

            MenuContainer.Children.Add(btn);
        }

        // Configuración especial para el botón de cerrar sesión
        private void AgregarBotonCerrarSesion()
        {
            var btnCerrar = new Button
            {
                Text = "🚪 Cerrar Sesión",
                BackgroundColor = Color.FromArgb("#FFEBEE"), // Fondo rojizo suave tipo alerta light
                TextColor = Color.FromArgb("#D32F2F"), // Texto rojo fuerte
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 8,
                Margin = new Thickness(0, 30, 0, 0)
            };

            btnCerrar.Clicked += async (s, e) =>
            {
                // Obtenemos el servicio de Google para destruir el token de sesión
                var authService = App.Services.GetService<PizzeriaApp.GoogleAuth.IGoogleAuthService>();

                if (authService != null)
                {
                    await authService.LogoutAsync();
                }

                // Mandamos al usuario de patitas a la calle (la pantalla de Login)
                var loginPage = App.Services.GetService<Login>();
                Application.Current.MainPage = new NavigationPage(loginPage);
            };

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