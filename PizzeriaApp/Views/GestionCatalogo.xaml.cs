using System;
using System.Threading.Tasks;
using PizzeriaApp.Models;
using PizzeriaApp.Controllers;
using PizzeriaApp.Config;

namespace PizzeriaApp.Views
{
    // Esta es la pantalla principal para el administrador, donde puede ver toda su carta de productos
    public partial class GestionCatalogo : ContentPage
    {
        private readonly DataBaseServices _dbService;
        private bool _isLoaded;

        public GestionCatalogo()
        {
            InitializeComponent();
            _dbService = new DataBaseServices();
        }

        // Refrescamos la lista cada vez que la pantalla aparece (por si venimos de editar o agregar)
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarInventarioTotalAsync();
        }

        // Jalamos todos los productos (activos e inactivos) desde Supabase
        private async Task CargarInventarioTotalAsync()
        {
            try
            {
                // Vamos por el catálogo completo al servidor
                var catalog = await _dbService.ObtenerCatalogoCompletoAsync();
                
                // Le pasamos la lista a nuestro ListView o CollectionView de la UI
                ListaInventario.ItemsSource = catalog;
            }
            catch (Exception ex)
            {
                // Siempre es bueno avisar si la red nos dejó colgados
                await DisplayAlert("Error", "No se pudo cargar el catálogo: " + ex.Message, "OK");
            }
        }

        // Si el admin pica el botón flotante o de barra para nueva pizza, lo mandamos para allá
        private async void OnAgregarProductoClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AltaProducto());
        }

        // Si toca un producto específico de la lista, lo mandamos a la edición pasando el objeto completo
        private async void OnEditarProductoTapped(object sender, TappedEventArgs e)
        {
            // El parámetro viene del BindingContext del elemento que tocaron
            var productoSeleccionado = e.Parameter as Producto;
            if (productoSeleccionado != null)
            {
                // Navegamos a la vista de edición inyectando el producto elegido
                await Navigation.PushAsync(new EditarProducto(productoSeleccionado));
            }
        }
    }
}
