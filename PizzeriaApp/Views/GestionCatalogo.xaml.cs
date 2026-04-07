using System;
using System.Threading.Tasks;
using PizzeriaApp.Models;
using PizzeriaApp.Controllers;
using PizzeriaApp.Config;

namespace PizzeriaApp.Views
{
    public partial class GestionCatalogo : ContentPage
    {
        private readonly DataBaseServices _dbService;
        private bool _isLoaded;

        public GestionCatalogo()
        {
            InitializeComponent();
            _dbService = new DataBaseServices();
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
                var catalog = await _dbService.ObtenerCatalogoCompletoAsync();
                ListaInventario.ItemsSource = catalog;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo cargar el catálogo: " + ex.Message, "OK");
            }
        }

        private async void OnAgregarProductoClicked(object sender, EventArgs e)
        {
            // Navegar a la vista de alta
            await Navigation.PushAsync(new AltaProducto());
        }

        private async void OnEditarProductoTapped(object sender, TappedEventArgs e)
        {
            var productoSeleccionado = e.Parameter as Producto;
            if (productoSeleccionado != null)
            {
                // Navegar a la vista de edición pasando el producto
                await Navigation.PushAsync(new EditarProducto(productoSeleccionado));
            }
        }
    }
}
