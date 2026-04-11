using PizzeriaApp.Controllers;
using PizzeriaApp.Models;
using PizzeriaApp.Services;

namespace PizzeriaApp.Views
{
    // Esta es la Vista de la Cocina; delega la orquestación al AdminController
    public partial class ColaCocina : ContentPage
    {
        private AdminController _controller;
        private DataBaseServices _dbRaw; // Para el realtime
        private bool _isLoading = false;

        public ColaCocina()
        {
            InitializeComponent();
            _dbRaw = new DataBaseServices();
            _controller = new AdminController(_dbRaw);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarPedidosAsync();

            // Suspensión en vivo: El controlador delega el canal de datos
            await _dbRaw.SuscribirseAPedidosEnVivo(payload =>
            {
                MainThread.BeginInvokeOnMainThread(async () => await CargarPedidosAsync());
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _dbRaw.DesuscribirsePedidosEnVivo();
        }

        private async Task CargarPedidosAsync()
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                loadingIndicator.IsRunning = true;
                loadingIndicator.IsVisible = true;

                // El controlador nos da la lista filtrada de pedidos activos
                var pedidos = await _controller.ObtenerColaCocinaAsync();

                ListaPedidosGrid.ItemsSource = pedidos;

                bool sinPedidos = pedidos == null || pedidos.Count == 0;
                emptyState.IsVisible = sinPedidos;
                ListaPedidosGrid.IsVisible = !sinPedidos;

                lblContador.Text = sinPedidos
                    ? "Cola de Cocina en Tiempo Real"
                    : $"{pedidos.Count} pedido{(pedidos.Count != 1 ? "s" : "")} en cola";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error en controlador de cocina: " + ex.Message);
            }
            finally
            {
                loadingIndicator.IsRunning = false;
                loadingIndicator.IsVisible = false;
                _isLoading = false;
            }
        }

        private async void OnRefreshClicked(object sender, EventArgs e)
        {
            await CargarPedidosAsync();
        }

        private async void OnPedidoTapped(object sender, TappedEventArgs e)
        {
            var border = sender as Border;
            var pedido = border?.BindingContext as Pedido;

            if (pedido != null)
            {
                await Navigation.PushAsync(new DetalleOrden(pedido, _dbRaw));
            }
        }

        private async void OnEstadoClicked(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var nuevoEstado = btn?.CommandParameter as string;
            var pedido = btn?.BindingContext as Pedido;

            if (pedido == null || string.IsNullOrEmpty(nuevoEstado)) return;

            btn.IsEnabled = false;

            // El controlador maneja la actualización y la notificación push al cliente
            bool ok = await _controller.ActualizarEstadoOrdenAsync(pedido.Id, pedido.ClienteId, pedido.IdVisible, nuevoEstado);
            
            if (ok)
            {
                await CargarPedidosAsync();
            }

            btn.IsEnabled = true;
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var pedido = btn?.BindingContext as Pedido;

            if (pedido == null) return;

            bool confirm = await DisplayAlert("Cancelar Pedido",
                $"¿Cancelar la orden {pedido.IdVisible}?", "Sí, cancelar", "No");

            if (confirm)
            {
                btn.IsEnabled = false;
                // Delegamos la cancelación al controlador (maneja estado + notificaciones)
                bool ok = await _controller.ActualizarEstadoOrdenAsync(pedido.Id, pedido.ClienteId, pedido.IdVisible, "Cancelado");
                if (ok)
                {
                    await CargarPedidosAsync();
                }
            }
        }
    }
}
