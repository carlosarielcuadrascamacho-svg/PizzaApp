using PizzeriaApp.Controllers;
using PizzeriaApp.Models;
using PizzeriaApp.Services;
using System.Collections.ObjectModel;

namespace PizzeriaApp.Views
{
    // En esta pantalla el cliente puede recordar todas las pizzas que ha pedido y ver en qué estado están
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

        // Cada vez que el cliente entra a ver sus pedidos, jalamos la data más reciente de Supabase
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarHistorialAsync();
        }

        // Método para traer el historial filtrado por el ID del usuario logueado
        private async Task CargarHistorialAsync()
        {
            try
            {
                // Vamos a la base de datos por todos los pedidos vinculados a este cliente
                var pedidos = await _dbService.ObtenerHistorialClienteAsync(_clienteId);
                // Pintamos la lista en la UI
                ListaHistorialGrid.ItemsSource = pedidos;
            }
            catch (Exception ex)
            {
                // Si la red falla, avisamos al usuario para que no piense que no tiene pedidos
                await DisplayAlert("Error", "No se pudo cargar tu historial de pedidos: " + ex.Message, "OK");
            }
        }

        // Evento por si el cliente se arrepiente y quiere cancelar antes de que lo preparen
        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var pedidoSeleccionado = btn?.CommandParameter as Pedido;

            if (pedidoSeleccionado == null) return;

            // Siempre confirmamos antes de hacer una acción destructiva de negocio
            bool confirm = await DisplayAlert("Confirmación", "¿Deseas cancelar este pedido?", "Sí, Cancelar", "No");
            if (confirm)
            {
                // Bloqueamos para evitar clics múltiples
                btn.IsEnabled = false;

                // Actualizamos el estado a "Cancelado" en el servidor
                bool ok = await _dbService.ActualizarEstadoPedidoAsync(pedidoSeleccionado.Id, "Cancelado");
                if (ok)
                {
                    // ¡Paso importante! Avisamos a los administradores mediante una push que el cliente canceló
                    // Así ellos no pierden tiempo preparando una pizza que ya no quieren
                    _ = NotificationService.NotificarCancelacionClienteAAdminsAsync(
                        _dbService, pedidoSeleccionado.IdVisible, _nombreCliente);

                    await DisplayAlert("Éxito", "Pedido cancelado correctamente.", "OK");
                    // Refrescamos la lista para que el estado se vea actualizado
                    await CargarHistorialAsync();
                }
                else
                {
                    // Si algo falló, rehabilitamos el botón para reintentar
                    btn.IsEnabled = true;
                    btn.Text = "CANCELAR PEDIDO";
                }
            }
        }
    }
}
