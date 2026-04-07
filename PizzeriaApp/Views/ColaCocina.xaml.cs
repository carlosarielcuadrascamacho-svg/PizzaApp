using PizzeriaApp.Controllers;
using PizzeriaApp.Models;

namespace PizzeriaApp.Views
{
    public partial class ColaCocina : ContentPage
    {
        private DataBaseServices _dbService;

        public ColaCocina()
        {
            InitializeComponent();
            _dbService = new DataBaseServices();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarPedidosAsync();
            
            // WebSockets de Supabase para actualizaciones en vivo
            _dbService.SuscribirseAPedidosEnVivo(async payload => {
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
            try
            {
                var pedidos = await _dbService.ObtenerPedidosActivosAsync();
                ListaPedidos.ItemsSource = pedidos;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        private async void OnEstadoClicked(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var nuevoEstado = btn?.CommandParameter as string;
            var pedido = btn?.BindingContext as Pedido; // BindingContext identifica la tarjeta actual

            if (pedido == null || string.IsNullOrEmpty(nuevoEstado)) return;

            bool ok = await _dbService.ActualizarEstadoPedidoAsync(pedido.Id, nuevoEstado);
            if (ok) await CargarPedidosAsync();
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var pedido = btn?.BindingContext as Pedido;

            if (pedido == null) return;

            bool confirm = await DisplayAlert("Cuidado", "¿Cancelar orden?", "Sí", "No");
            if (confirm)
            {
                await _dbService.ActualizarEstadoPedidoAsync(pedido.Id, "Cancelado");
                await CargarPedidosAsync();
            }
        }
    }
}
