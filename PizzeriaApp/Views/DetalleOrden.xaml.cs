using System;
using PizzeriaApp.Controllers;
using PizzeriaApp.Models;
using PizzeriaApp.Services;

namespace PizzeriaApp.Views
{
    public partial class DetalleOrden : ContentPage
    {
        private readonly Pedido _pedido;
        private readonly DataBaseServices _dbService;

        public DetalleOrden(Pedido pedido, DataBaseServices dbService)
        {
            InitializeComponent();
            _pedido = pedido;
            _dbService = dbService;

            CargarDatosOrden();
            _ = CargarDatosClienteAsync();
        }

        private void CargarDatosOrden()
        {
            // Header
            lblOrdenId.Text = _pedido.IdVisible;
            lblMesa.Text = _pedido.MesaVisible;
            lblEstado.Text = _pedido.Estado;
            badgeEstado.BackgroundColor = Color.FromArgb(_pedido.ColorEstado);
            lblFecha.Text = _pedido.Fecha.ToLocalTime().ToString("dd MMM yyyy • HH:mm");
            lblTiempo.Text = _pedido.TiempoRelativo;

            // Total
            lblTotal.Text = _pedido.Total.ToString("C");

            // Productos
            listaProductos.Children.Clear();
            foreach (var detalle in _pedido.Detalles)
            {
                var itemGrid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection
                    {
                        new ColumnDefinition(new GridLength(50)),
                        new ColumnDefinition(GridLength.Star),
                        new ColumnDefinition(GridLength.Auto)
                    },
                    Padding = new Thickness(5, 4)
                };

                itemGrid.Add(new Label
                {
                    Text = $"x{detalle.Cantidad}",
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#FF4B3A"),
                    VerticalOptions = LayoutOptions.Center
                }, 0);

                itemGrid.Add(new Label
                {
                    Text = detalle.NombrePlatillo,
                    FontSize = 15,
                    TextColor = Color.FromArgb("#333"),
                    VerticalOptions = LayoutOptions.Center
                }, 1);

                itemGrid.Add(new Label
                {
                    Text = detalle.Subtotal.ToString("C"),
                    FontSize = 14,
                    TextColor = Color.FromArgb("#757575"),
                    VerticalOptions = LayoutOptions.Center
                }, 2);

                listaProductos.Children.Add(itemGrid);
            }

            // Comentario
            if (!string.IsNullOrWhiteSpace(_pedido.Comentario))
            {
                panelComentario.IsVisible = true;
                lblComentario.Text = _pedido.Comentario;
            }

            // Acciones solo si el pedido está activo
            panelAcciones.IsVisible = _pedido.IsNotCancelled;
        }

        private async Task CargarDatosClienteAsync()
        {
            try
            {
                var cliente = await _dbService.ObtenerPerfilAsync(_pedido.ClienteId);

                if (cliente != null)
                {
                    lblClienteNombre.Text = !string.IsNullOrEmpty(cliente.Nombre) ? cliente.Nombre : "Sin nombre";
                    lblClienteTelefono.Text = !string.IsNullOrEmpty(cliente.Telefono) ? cliente.Telefono : "No registrado";
                    lblClienteEmail.Text = !string.IsNullOrEmpty(cliente.Email) ? cliente.Email : "No registrado";
                    lblClienteDireccion.Text = !string.IsNullOrEmpty(cliente.Direccion) ? cliente.Direccion : "No registrada";
                }
                else
                {
                    lblClienteNombre.Text = "Cliente no encontrado";
                    lblClienteTelefono.Text = "--";
                    lblClienteEmail.Text = "--";
                    lblClienteDireccion.Text = "--";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cargando datos del cliente: {ex.Message}");
                lblClienteNombre.Text = "Error al cargar";
            }
            finally
            {
                loadingCliente.IsRunning = false;
                loadingCliente.IsVisible = false;
                panelDatosCliente.IsVisible = true;
            }
        }

        private async void OnCambiarEstado(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var nuevoEstado = btn?.CommandParameter as string;

            if (string.IsNullOrEmpty(nuevoEstado)) return;

            btn.IsEnabled = false;

            bool ok = await _dbService.ActualizarEstadoPedidoAsync(_pedido.Id, nuevoEstado);

            if (ok)
            {
                // Notificar al cliente del cambio de estado
                _ = NotificationService.NotificarCambioEstadoAClienteAsync(
                    _dbService, _pedido.ClienteId, nuevoEstado, _pedido.IdVisible);

                await DisplayAlert("Actualizado", $"La orden ahora está: {nuevoEstado}", "OK");
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert("Error", "No se pudo actualizar el estado.", "OK");
                btn.IsEnabled = true;
            }
        }

        private async void OnCancelarOrden(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Cancelar Pedido",
                $"¿Cancelar la orden {_pedido.IdVisible}?", "Sí, cancelar", "No");

            if (confirm)
            {
                btnCancelar.IsEnabled = false;
                bool ok = await _dbService.ActualizarEstadoPedidoAsync(_pedido.Id, "Cancelado");

                if (ok)
                {
                    // Notificar al cliente de la cancelación
                    _ = NotificationService.NotificarCambioEstadoAClienteAsync(
                        _dbService, _pedido.ClienteId, "Cancelado", _pedido.IdVisible);

                    await DisplayAlert("Cancelado", "La orden ha sido cancelada.", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Error", "No se pudo cancelar.", "OK");
                    btnCancelar.IsEnabled = true;
                }
            }
        }
    }
}
