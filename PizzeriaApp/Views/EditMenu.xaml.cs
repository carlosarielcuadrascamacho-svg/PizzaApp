using System;
using System.Threading.Tasks;
using PizzeriaApp.Models;
using PizzeriaApp.Controllers;

namespace PizzeriaApp.Views
{
    public partial class EditMenu : ContentPage
    {
        private readonly DataBaseServices _dbService;
        private bool _isLoaded;

        public EditMenu()
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
            // Para el modo Admin solicitamos el array general de la DB (se usa el Activo solo como flag visual)
            var inventario = await _dbService.ObtenerProductosActivosAsync();
            ListaInventario.ItemsSource = inventario;
            _isLoaded = true;
        }

        private async void OnDisponibilidadToggled(object sender, ToggledEventArgs e)
        {
            if (!_isLoaded) return;
            
            var controlSwitch = sender as Switch;
            var productoModificado = controlSwitch?.BindingContext as Producto;

            if (productoModificado != null)
            {
                // Aquí deberías colocar la llamada abstracta a Supabase para UPDATE tbl_productos
                // await _dbService.ActualizarDisponibilidadProducto(productoModificado.Id, e.Value);
                await DisplayAlert("Gestión de Catálogo", $"Se ha modificado la disponibilidad de {productoModificado.Nombre}.", "OK");
            }
        }
    }
}
