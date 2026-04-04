using PizzeriaApp.Models;
using PizzeriaApp.Controllers;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Maui.ApplicationModel; // Necesario para MainThread
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace PizzeriaApp.Views
{
    public partial class MenuAdmin : ContentPage
    {
        private readonly DataBaseServices _dbService;
        private ObservableCollection<Pedido> _pedidosEnVivo;

        public MenuAdmin()
        {
            InitializeComponent();

            // Obtener servicio desde el contenedor DI de la aplicación (via MauiContext)
            var svc = Application.Current?.Handler?.MauiContext?.Services?.GetService<DataBaseServices>();
            _dbService = svc ?? new DataBaseServices();

            _pedidosEnVivo = new ObservableCollection<Pedido>();

            // Enlazamos la colección a la tabla una sola vez
            GridPedidos.ItemsSource = _pedidosEnVivo;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            // 1. Cargamos el estado inicial (Solo pedidos no entregados)
            await CargarPedidosSegunFiltro("Activos");

            // 2. Encendemos el túnel de WebSockets para escuchar el futuro
            // Aseguramos que el cliente esté inicializado antes de suscribirnos
            try
            {
                await _dbService.InitializeAsync();
                await _dbService.SuscribirseAPedidosEnVivo(RecibirPedidoNube);
            }
            catch (Exception ex)
            {
                // Log y mostrar alerta si es necesario
                System.Diagnostics.Debug.WriteLine($"Error al inicializar suscripción en vivo: {ex.Message}");
                await DisplayAlert("Error", "No se pudo conectar a los pedidos en vivo.", "OK");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Desuscribirse para prevenir memory leaks y crashes al regresar a la vista
            _dbService.DesuscribirsePedidosEnVivo();
        }

        private async Task CargarPedidosSegunFiltro(string filtro)
        {
            var pedidos = new List<Pedido>();

            if (filtro == "Completados")
            {
                // Un método nuevo en BD para completados
                pedidos = await _dbService.ObtenerPedidosCompletadosAsync();
            }
            else
            {
                pedidos = await _dbService.ObtenerPedidosActivosAsync();
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Desligamos el Grid temporalmente para evitar que detecte todas las inserciones asíncronas y truene en Android
                GridPedidos.ItemsSource = null;

                _pedidosEnVivo.Clear();
                foreach (var p in pedidos)
                {
                    _pedidosEnVivo.Add(p);
                }

                GridPedidos.ItemsSource = _pedidosEnVivo;
            });
        }

        // Este método es invocado por la base de datos desde la nube
        private void RecibirPedidoNube(Pedido nuevoPedido)
        {
            // ALERTA DE ARQUITECTURA: Los WebSockets corren en hilos de fondo. 
            // Para pintar algo en la pantalla (UI), debemos forzar a que ocurra en el Hilo Principal.
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Agregamos el nuevo pedido al principio de la lista
                _pedidosEnVivo.Insert(0, nuevoPedido);
            });
        }

        private async void OnDespacharClicked(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var pedidoTerminado = btn?.CommandParameter as Pedido;

            if (pedidoTerminado != null)
            {
                // Bloqueamos el botón visualmente para evitar clics dobles
                btn.IsEnabled = false;

                // Actualizamos la base de datos
                bool exito = await _dbService.ActualizarEstadoPedidoAsync(pedidoTerminado.Id, "Entregado");
                
                // Actualizamos el objeto localmente para reflejar el cambio
                pedidoTerminado.Estado = "Entregado";

                if (exito)
                {
                    // Lo removemos de la tabla visualmente si estamos en la vista de activos
                    _pedidosEnVivo.Remove(pedidoTerminado);
                }
                else
                {
                    btn.IsEnabled = true;
                    await DisplayAlert("Error", "No se pudo actualizar el pedido en el servidor.", "OK");
                }
            }
        }

        private async void OnFiltroChanged(object sender, EventArgs e)
        {
            var picker = sender as Picker;
            var seleccion = picker?.SelectedItem as string;

            if (!string.IsNullOrEmpty(seleccion))
            {
                await CargarPedidosSegunFiltro(seleccion);
            }
        }
    }
}
