using PizzeriaApp.Models;
using PizzeriaApp.Controllers;
using System.Collections.ObjectModel;
using System.Linq;

namespace PizzeriaApp.Views
{
    public partial class MenuClient : ContentPage
    {
        private readonly UsuarioPerfil _clienteActual;
        private readonly DataBaseServices _dbService;

        // Estructuras de datos para manejar el estado visual
        private ObservableCollection<ItemCarrito> _carrito;
        private decimal _totalCarrito = 0;

        public MenuClient(UsuarioPerfil cliente)
        {
            InitializeComponent();
            _clienteActual = cliente;
            _dbService = new DataBaseServices();
            _carrito = new ObservableCollection<ItemCarrito>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            // Disparamos la consulta a PostgreSQL al abrir la pantalla
            await CargarCatalogoAsync();
        }

        private async Task CargarCatalogoAsync()
        {
            var productos = await _dbService.ObtenerProductosActivosAsync();
            ListaProductos.ItemsSource = productos;
        }

        private void OnAgregarAlCarritoClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            // El XAML nos envía el objeto completo usando el CommandParameter
            var productoSeleccionado = button?.CommandParameter as Producto;

            if (productoSeleccionado != null)
            {
                // Buscamos si la pizza ya estaba en el carrito
                var itemExistente = _carrito.FirstOrDefault(i => i.Producto.Id == productoSeleccionado.Id);

                if (itemExistente != null)
                {
                    itemExistente.Cantidad++;
                }
                else
                {
                    _carrito.Add(new ItemCarrito { Producto = productoSeleccionado, Cantidad = 1 });
                }

                ActualizarResumenCarrito();
            }
        }

        private void ActualizarResumenCarrito()
        {
            // LINQ para calcular dinámicamente el estado
            int totalArticulos = _carrito.Sum(i => i.Cantidad);
            _totalCarrito = _carrito.Sum(i => i.Subtotal);

            lblArticulos.Text = $"{totalArticulos} artículos";
            lblTotal.Text = $"Total: {_totalCarrito:C}";

            // Bloqueamos el botón si la lista de la compra está vacía
            btnOrdenar.IsEnabled = totalArticulos > 0;
        }

        private async void OnConfirmarOrdenClicked(object sender, EventArgs e)
        {
            btnOrdenar.IsEnabled = false;
            btnOrdenar.Text = "Enviando a cocina...";

            // Disparamos la arquitectura de red hacia Supabase
            bool exito = await _dbService.CrearPedidoCompletoAsync(_clienteActual.Id, _carrito.ToList(), _totalCarrito);

            if (exito)
            {
                await DisplayAlert("¡Éxito!", "Tu pedido ha sido procesado correctamente.", "Aceptar");

                // Vaciamos el carrito para dejar la pantalla lista para otra compra
                _carrito.Clear();
                ActualizarResumenCarrito();
            }
            else
            {
                await DisplayAlert("Error", "No pudimos procesar tu pedido. Intenta de nuevo.", "Ok");
            }

            btnOrdenar.Text = "Confirmar Orden";
        }
    }
}