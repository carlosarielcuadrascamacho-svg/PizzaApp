using PizzeriaApp.Controllers;
using PizzeriaApp.Models;
using PizzeriaApp.Services;

namespace PizzeriaApp.Views
{
    // Esta es la pantalla que ven en la cocina para saber qué pizzas tienen que preparar hoy
    public partial class ColaCocina : ContentPage
    {
        private DataBaseServices _dbService;
        private bool _isLoading = false;

        public ColaCocina()
        {
            InitializeComponent();
            _dbService = new DataBaseServices();
        }

        // Cada vez que el cocinero entra a la pantalla, jalamos la data y nos conectamos al "en vivo"
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarPedidosAsync();

            // Nos suscribimos a Supabase Realtime para que la lista se mueva solita cuando lleguen pedidos nuevos
            await _dbService.SuscribirseAPedidosEnVivo(payload =>
            {
                // Como esto viene de un hilo de fondo, forzamos el refresco en el hilo principal de la UI
                MainThread.BeginInvokeOnMainThread(async () => await CargarPedidosAsync());
            });
        }

        // Importante: si salimos de la pantalla, hay que cerrar la conexión para no gastar recursos
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _dbService.DesuscribirsePedidosEnVivo();
        }

        // El motor principal para traer los pedidos que no han sido entregados ni cancelados
        private async Task CargarPedidosAsync()
        {
            if (_isLoading) return; // Un semáforo para no encimar peticiones si el internet está lento
            _isLoading = true;

            try
            {
                // Prendemos el circulito de carga
                loadingIndicator.IsRunning = true;
                loadingIndicator.IsVisible = true;

                // Vamos por los pedidos frescos a la base de datos
                var pedidos = await _dbService.ObtenerPedidosActivosAsync();

                // Pintamos los pedidos en nuestra vista
                ListaPedidosGrid.ItemsSource = pedidos;

                // Si no hay nada, mostramos un mensaje amigable de "No hay trabajo por ahora"
                bool sinPedidos = pedidos == null || pedidos.Count == 0;
                emptyState.IsVisible = sinPedidos;
                ListaPedidosGrid.IsVisible = !sinPedidos;

                // Actualizamos el contador dinámico del título
                lblContador.Text = sinPedidos
                    ? "Cola de Cocina en Tiempo Real"
                    : $"{pedidos.Count} pedido{(pedidos.Count != 1 ? "s" : "")} en cola";
            }
            catch (Exception ex)
            {
                // Si algo truena, lo mandamos a consola para no asustar al cocinero con un popup feo
                Console.WriteLine("Error cargando pedidos: " + ex.Message);
            }
            finally
            {
                // Limpieza final de los indicadores de carga
                loadingIndicator.IsRunning = false;
                loadingIndicator.IsVisible = false;
                _isLoading = false;
            }
        }

        // Botón manual de refresco, por si las dudas
        private async void OnRefreshClicked(object sender, EventArgs e)
        {
            await CargarPedidosAsync();
        }

        // Si el cocinero quiere ver el detalle completo (qué ingredientes lleva la pizza)
        private async void OnPedidoTapped(object sender, TappedEventArgs e)
        {
            var border = sender as Border;
            var pedido = border?.BindingContext as Pedido;

            if (pedido != null)
            {
                // Mandamos a la pantalla de detalle pasando el pedido seleccionado
                await Navigation.PushAsync(new DetalleOrden(pedido, _dbService));
            }
        }

        // Este evento maneja los cambios de estado: de "En preparación" a "Listo", etc.
        private async void OnEstadoClicked(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var nuevoEstado = btn?.CommandParameter as string;
            var pedido = btn?.BindingContext as Pedido;

            if (pedido == null || string.IsNullOrEmpty(nuevoEstado)) return;

            // Bloqueamos para que no le piquen dos veces por accidente
            btn.IsEnabled = false;

            // Actualizamos en la base de datos
            bool ok = await _dbService.ActualizarEstadoPedidoAsync(pedido.Id, nuevoEstado);
            if (ok)
            {
                // ¡Paso clave! Mandamos una notificación push al celular del cliente para avisarle
                _ = NotificationService.NotificarCambioEstadoAClienteAsync(
                    _dbService, pedido.ClienteId, nuevoEstado, pedido.IdVisible);

                // Refrescamos la lista local
                await CargarPedidosAsync();
            }

            btn.IsEnabled = true;
        }

        // Por si se quemó la pizza o hubo un problema, el admin puede cancelar desde aquí
        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var pedido = btn?.BindingContext as Pedido;

            if (pedido == null) return;

            // Pedimos confirmación porque una cancelación es algo serio
            bool confirm = await DisplayAlert("Cancelar Pedido",
                $"¿Cancelar la orden {pedido.IdVisible}?", "Sí, cancelar", "No");

            if (confirm)
            {
                btn.IsEnabled = false;
                // Marcamos como cancelado en Supabase
                bool ok = await _dbService.ActualizarEstadoPedidoAsync(pedido.Id, "Cancelado");
                if (ok)
                {
                    // Le avisamos al cliente con una push de que su pedido fue cancelado
                    _ = NotificationService.NotificarCambioEstadoAClienteAsync(
                        _dbService, pedido.ClienteId, "Cancelado", pedido.IdVisible);
                }
                await CargarPedidosAsync();
            }
        }
    }
}
