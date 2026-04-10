using PizzeriaApp.Controllers;
using PizzeriaApp.Models;
using PizzeriaApp.Services;
using System.Collections.ObjectModel;

namespace PizzeriaApp.Views
{
    public partial class HistorialCliente : ContentPage
    {
        private string _clienteId;
        private string _nombreCliente;
        private DataBaseServices _dbService;

        public HistorialCliente(string clienteId, string nombreCliente = "Cliente")
        {
            InitializeComponent();
            _clienteId = clienteId;
            _nombreCliente = nombreCliente;
            _dbService = new DataBaseServices();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarHistorialAsync();
        }

        private async Task CargarHistorialAsync()
        {
            try
            {
                var pedidos = await _dbService.ObtenerHistorialClienteAsync(_clienteId);
                ListaHistorial.ItemsSource = pedidos;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se cargó el historial: " + ex.Message, "OK");
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
                bool ok = await _dbService.ActualizarEstadoPedidoAsync(pedidoSeleccionado.Id, "Cancelado");
                if (ok)
                {
                    // Notificar a los admins que el cliente canceló su pedido
                    _ = NotificationService.NotificarCancelacionClienteAAdminsAsync(
                        _dbService, pedidoSeleccionado.IdVisible, _nombreCliente);

                    await DisplayAlert("Éxito", "Pedido cancelado.", "OK");
                    await CargarHistorialAsync();
                }
                else
                {
                    btn.IsEnabled = true;
                    btn.Text = "CANCELAR PEDIDO";
                }
            }
        }
    }
}
