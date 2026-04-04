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
            _isLoaded = false;
            // Descargar el catálogo completo ignorando flag activo
            var catalog = await _dbService.ObtenerCatalogoCompletoAsync();
            ListaInventario.ItemsSource = catalog;
            _isLoaded = true;
        }

        private async void OnDisponibilidadToggled(object sender, ToggledEventArgs e)
        {
            if (!_isLoaded) return;
            
            var controlSwitch = sender as Switch;
            var productoModificado = controlSwitch?.BindingContext as Producto;

            if (productoModificado != null)
            {
                // Actualiza in-memory boolean property and Supabase
                productoModificado.Activo = e.Value;
                bool dbExito = await _dbService.CambiarEstadoProductoAsync(productoModificado.Id, e.Value);
                if(!dbExito)
                {
                    await DisplayAlert("Error", "No se pudo cambiar la disponibilidad en Supabase.", "OK");
                    controlSwitch.IsToggled = !e.Value; // Revertir visualmente
                }
            }
        }
    }
}
