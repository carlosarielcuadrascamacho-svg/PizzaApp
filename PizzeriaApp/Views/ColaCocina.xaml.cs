using PizzeriaApp.Controllers;
using PizzeriaApp.Models;

namespace PizzeriaApp.Views
{
    public partial class ColaCocina : ContentPage
    {
        private DataBaseServices _dbService;
        private bool _isLoading = false;

        public ColaCocina()
        {
            InitializeComponent();
            _dbService = new DataBaseServices();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarPedidosAsync();

            // Escuchar cambios en tiempo real (INSERTs y UPDATEs)
            await _dbService.SuscribirseAPedidosEnVivo(payload =>
            {
                MainThread.BeginInvokeOnMainThread(async () => await CargarPedidosAsync());
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _dbService.DesuscribirsePedidosEnVivo();
        }

        private async Task CargarPedidosAsync()
        {
            if (_isLoading) return; // Evitar llamadas concurrentes
            _isLoading = true;

            try
            {
                loadingIndicator.IsRunning = true;
                loadingIndicator.IsVisible = true;

                var pedidos = await _dbService.ObtenerPedidosActivosAsync();

                ListaPedidos.ItemsSource = pedidos;

                // Mostrar estado vacío o la lista
                bool sinPedidos = pedidos == null || pedidos.Count == 0;
                emptyState.IsVisible = sinPedidos;
                ListaPedidos.IsVisible = !sinPedidos;

                // Actualizar contador
                lblContador.Text = sinPedidos
                    ? "Cola de Cocina en Tiempo Real"
                    : $"{pedidos.Count} pedido{(pedidos.Count != 1 ? "s" : "")} en cola";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error cargando pedidos: " + ex.Message);
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
                await Navigation.PushAsync(new DetalleOrden(pedido, _dbService));
            }
        }

        private async void OnEstadoClicked(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var nuevoEstado = btn?.CommandParameter as string;
            var pedido = btn?.BindingContext as Pedido;

            if (pedido == null || string.IsNullOrEmpty(nuevoEstado)) return;

            // Bloquear el botón para evitar doble clic
            btn.IsEnabled = false;

            bool ok = await _dbService.ActualizarEstadoPedidoAsync(pedido.Id, nuevoEstado);
            if (ok) await CargarPedidosAsync();

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
                await _dbService.ActualizarEstadoPedidoAsync(pedido.Id, "Cancelado");
                await CargarPedidosAsync();
            }
        }
    }
}
