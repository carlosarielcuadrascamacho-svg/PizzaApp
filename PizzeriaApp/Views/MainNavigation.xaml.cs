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

        private void ConfigurarMenu(UsuarioPerfil usuario)
        {
            // 1. Personalizamos la UI con los datos del usuario
            lblNombreUsuario.Text = $"Hola, {usuario.Id}";

            // Limpiamos el contenedor por seguridad
            MenuContainer.Children.Clear();

            // 2. Evaluamos el rol (Lógica de presentación simple)
            if (usuario.EsAdmin)
            {
                // Vistas para el perfil Administrador
                AgregarBotonMenu("Ver Pedidos", new MenuAdmin(usuario.Id));
                // AgregarBotonMenu("Reportes", new ReportesAdmin()); // Lo descomentarás cuando crees esta vista
            }
            else if (usuario.EsAdmin)
            {
                // Vistas para el perfil Cliente
                AgregarBotonMenu("Realizar Pedido", new MenuClient(usuario.Id));
                // AgregarBotonMenu("Mi Historial", new HistorialClient()); // Lo descomentarás cuando crees esta vista
            }

            // 3. Siempre agregamos la opción de salir al final
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

            btnCerrar.Clicked += (s, e) =>
            {
                // Al cerrar sesión, destruimos la navegación actual y volvemos al Login
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