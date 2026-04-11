using System;
using System.Threading.Tasks;
using PizzeriaApp.Models;
using PizzeriaApp.Controllers;
using PizzeriaApp.Services;

namespace PizzeriaApp.Views
{
    // Esta es la Vista de Gestión; delega el control del inventario al AdminController
    public partial class GestionCatalogo : ContentPage
    {
        private readonly AdminController _controller;

        public GestionCatalogo()
        {
            InitializeComponent();
            // Inicialización del controlador administrativo
            _controller = new AdminController(new DataBaseServices());
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarInventarioTotalAsync();
        }

        private async Task CargarInventarioTotalAsync()
        {
            try
            {
                // El controlador nos da el catálogo total (incluyendo inactivos)
                var catalog = await _controller.ObtenerTodoElCatalogoAsync();
                ListaInventario.ItemsSource = catalog;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo cargar el catálogo: " + ex.Message, "OK");
            }
        }

        private async void OnAgregarProductoClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AltaProducto());
        }

        private async void OnEditarProductoTapped(object sender, TappedEventArgs e)
        {
            var productoSeleccionado = e.Parameter as Producto;
            if (productoSeleccionado != null)
            {
                await Navigation.PushAsync(new EditarProducto(productoSeleccionado));
            }
        }
    }
}
