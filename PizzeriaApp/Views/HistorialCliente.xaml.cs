using PizzeriaApp.Controllers;
using PizzeriaApp.Models;
using PizzeriaApp.Services;
using System.Collections.ObjectModel;

namespace PizzeriaApp.Views
{
    // Esta es la Vista del Historial; delega la orquestación de datos al OrderController
    public partial class HistorialCliente : ContentPage
    {
        private string _clienteId;
        private string _nombreCliente;
        private OrderController _controller;

        public HistorialCliente(string clienteId, string nombreCliente = "Cliente")
        {
            InitializeComponent();
            _clienteId = clienteId;
            _nombreCliente = nombreCliente;
            // Inicialización del controlador de pedidos
            _controller = new OrderController(new DataBaseServices());
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarHistorialAsync();
        }

        private void ListaHistorialGrid_QueryRowHeight(object sender, Syncfusion.Maui.DataGrid.DataGridQueryRowHeightEventArgs e)
        {
            if (e.RowIndex > 0)
            {
                e.Height = e.GetIntrinsicRowHeight(e.RowIndex);
                e.Handled = true;
            }
        }

        private async Task CargarHistorialAsync()
        {
            try
            {
                // El controlador nos da el historial filtrado por cliente
                var pedidos = await _controller.ObtenerHistorialClienteAsync(_clienteId);
                ListaHistorialGrid.ItemsSource = pedidos;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo cargar tu historial: " + ex.Message, "OK");
            }
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var pedidoSeleccionado = btn?.CommandParameter as Pedido;

            if (pedidoSeleccionado == null) return;

            bool confirm = await DisplayAlert("Confirmación", "¿Deseas cancelar este pedido?", "Sí, Cancelar", "No");
            if (confirm)
            {
                btn.IsEnabled = false;

                // Delegamos la cancelación al controlador
                bool ok = await _controller.CancelarOrdenAsync(pedidoSeleccionado);
                
                if (ok)
                {
                    // Notificamos a los admins (lógica de negocio específica)
                    _ = NotificationService.NotificarCancelacionClienteAAdminsAsync(
                        new DataBaseServices(), pedidoSeleccionado.IdVisible, _nombreCliente);

                    await DisplayAlert("Éxito", "Pedido cancelado correctamente.", "OK");
                    await CargarHistorialAsync();
                }
                else
                {
                    btn.IsEnabled = true;
                }
            }
        }
    }
}
