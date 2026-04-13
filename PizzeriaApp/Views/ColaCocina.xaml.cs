using PizzeriaApp.Controllers;
using PizzeriaApp.Models;
using PizzeriaApp.Services;

namespace PizzeriaApp.Views
{
    // Esta es la Vista de la Cocina; delega la orquestación al AdminController
    public partial class ColaCocina : ContentPage
    {
        private AdminController _controller;
        private ServicioPedidos _servicioPedidos; 
        private bool _isLoading = false;
        
        // Usamos ObservableCollection para que el DataGrid se entere de cambios individuales
        private System.Collections.ObjectModel.ObservableCollection<Pedido> _pedidosEnPantalla = new();
        private List<Pedido> _pedidosCompletosFull = new List<Pedido>();

        public ColaCocina()
        {
            InitializeComponent();
            _servicioPedidos = new ServicioPedidos();
            // El controlador requiere los 3 servicios especializados
            _controller = new AdminController(new ServicioReportes(), _servicioPedidos, new ServicioCatalogo());
            
            // Vinculamos la colección una sola vez
            ListaPedidosGrid.ItemsSource = _pedidosEnPantalla;

            // Iniciamos el timer de urgencia
            IniciarTimerRefresco();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarPedidosAsync();

            // Sincronización inteligente por socket usando el nuevo servicio
            await _servicioPedidos.SuscribirseAPedidosEnVivo(pedidoCambiado =>
            {
                // Procesamos el cambio sutilmente sin recargar todo el catálogo
                MainThread.BeginInvokeOnMainThread(() => ProcesarCambioEnVivo(pedidoCambiado));
            });
        }

        private void ProcesarCambioEnVivo(Pedido pedido)
        {
            if (pedido == null) return;

            var existente = _pedidosEnPantalla.FirstOrDefault(p => p.Id == pedido.Id);

            // Si el pedido ya no debe estar en cocina (Entregado o Cancelado)
            if (pedido.Estado == "Entregado" || pedido.Estado == "Cancelado")
            {
                if (existente != null) _pedidosEnPantalla.Remove(existente);
            }
            else
            {
                // Si ya existe, actualizamos su estado (el modelo avisará a la UI automáticamente)
                if (existente != null)
                {
                    existente.Estado = pedido.Estado;
                }
                else
                {
                    // Si es un pedido nuevo que entró mientras estábamos aquí, lo agregamos al principio
                    _pedidosEnPantalla.Insert(0, pedido);
                    // Como es nuevo, necesitamos recargar los detalles (ingredientes)
                    _ = CargarPedidosAsync(); 
                }
            }

            ActualizarInterfazFullCheck();
        }

        private void ActualizarInterfazFullCheck()
        {
            bool sinPedidos = _pedidosEnPantalla.Count == 0;
            emptyState.IsVisible = sinPedidos;
            ListaPedidosGrid.IsVisible = !sinPedidos;
            
            lblContador.Text = sinPedidos ? "Panel de Cocina" : $"{_pedidosEnPantalla.Count} pedido{(_pedidosEnPantalla.Count != 1 ? "s" : "")} en cola";
            
            // Actualizamos la lista de ingredientes totales en preparación
            ActualizarResumenCarga(_pedidosEnPantalla.ToList());
        }

        private void IniciarTimerRefresco()
        {
            Device.StartTimer(TimeSpan.FromMinutes(1), () =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    foreach (var p in _pedidosEnPantalla) p.NotifyUrgencyChanged();
                });
                return true;
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _servicioPedidos.DesuscribirsePedidosEnVivo();
        }

        private async Task CargarPedidosAsync()
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                if (!_pedidosEnPantalla.Any())
                {
                    loadingIndicator.IsRunning = true;
                    loadingIndicator.IsVisible = true;
                }

                var nuevosPedidos = await _controller.ObtenerColaCocinaAsync();
                _pedidosCompletosFull = nuevosPedidos;
                
                SincronizarListaInicial(nuevosPedidos);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error cargando cocina: " + ex.Message);
            }
            finally
            {
                loadingIndicator.IsRunning = false;
                loadingIndicator.IsVisible = false;
                _isLoading = false;
            }
        }

        private void SincronizarListaInicial(List<Pedido> nuevos)
        {
            // Limpieza básica para la carga inicial/manual
            _pedidosEnPantalla.Clear();
            foreach (var p in nuevos) _pedidosEnPantalla.Add(p);
            
            ActualizarInterfazFullCheck();
        }

        private void ActualizarResumenCarga(List<Pedido> pedidos)
        {
            var pendientes = pedidos.Where(p => p.Estado == "Ordenado" || p.Estado == "En preparación").ToList();
            
            var resumen = pendientes
                .SelectMany(p => p.Detalles)
                .GroupBy(d => d.NombrePlatillo)
                .Select(g => new { Nombre = g.Key, Total = g.Sum(x => x.Cantidad) })
                .OrderByDescending(x => x.Total)
                .ToList();

            LayoutResumen.Children.Clear();
            foreach (var item in resumen)
            {
                var chip = new Border {
                    Padding = new Thickness(12, 6),
                    BackgroundColor = Color.FromArgb("#1A1A1A"),
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 15 },
                    Content = new HorizontalStackLayout {
                        Spacing = 5,
                        Children = {
                            new Label { Text = $"{item.Total}x", TextColor = Color.FromArgb("#FF4B3A"), FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center },
                            new Label { Text = item.Nombre, TextColor = Colors.White, FontSize = 12, VerticalOptions = LayoutOptions.Center }
                        }
                    }
                };
                LayoutResumen.Children.Add(chip);
            }
        }

        private void OnFilterClicked(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var filtro = btn?.CommandParameter as string;

            if (filtro == "Todos") SincronizarListaInicial(_pedidosCompletosFull);
            else SincronizarListaInicial(_pedidosCompletosFull.Where(p => p.Estado == filtro).ToList());
        }

        private async void OnRefreshClicked(object sender, EventArgs e)
        {
            await CargarPedidosAsync();
        }

        private void OnTicketTapped(object sender, EventArgs e)
        {
            if (sender is View view && view.BindingContext is Pedido pedido)
            {
                pedido.IsExpanded = !pedido.IsExpanded;
            }
        }

        private async void OnEstadoClicked(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var nuevoEstado = btn?.CommandParameter as string;
            var pedido = btn?.BindingContext as Pedido;

            if (pedido == null || string.IsNullOrEmpty(nuevoEstado)) return;

            btn.IsEnabled = false;

            // Actualización optimista: cambiamos el estado localmente para respuesta inmediata
            var estadoAnterior = pedido.Estado;
            pedido.Estado = nuevoEstado;

            bool ok = await _controller.ActualizarEstadoOrdenAsync(pedido.Id, pedido.ClienteId, pedido.IdVisible, nuevoEstado);
            
            if (!ok)
            {
                // Si el servidor falla, regresamos el estado anterior y avisamos
                pedido.Estado = estadoAnterior;
                await DisplayAlert("Error", "No se pudo actualizar el estado en el servidor. Revisa tu conexión.", "Ok");
            }
            // NOTA: No llamamos a CargarPedidosAsync() porque el socket (Realtime) se encargará de confirmar el cambio
            
            btn.IsEnabled = true;
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var pedido = btn?.BindingContext as Pedido;

            if (pedido == null) return;

            bool confirm = await DisplayAlert("Cancelar Pedido", $"¿Cancelar la orden {pedido.IdVisible}?", "Sí, cancelar", "No");

            if (confirm)
            {
                btn.IsEnabled = false;
                bool ok = await _controller.ActualizarEstadoOrdenAsync(pedido.Id, pedido.ClienteId, pedido.IdVisible, "Cancelado");
                // El socket lo eliminará de la lista automáticamente al recibir el estado "Cancelado"
                btn.IsEnabled = true;
            }
        }
    }
}
