using System;
using PizzeriaApp.Controllers;
using PizzeriaApp.Models;
using PizzeriaApp.Services;

namespace PizzeriaApp.Views
{
    // Esta es la Vista de Detalle; delega la orquestación de datos al AdminController
    public partial class DetalleOrden : ContentPage
    {
        private readonly Pedido _pedido;
        private readonly AdminController _controller;

        public DetalleOrden(Pedido pedido, DataBaseServices dbService)
        {
            InitializeComponent();
            _pedido = pedido;
            // Inicialización del controlador administrativo
            _controller = new AdminController(dbService);

            CargarDatosOrden();
            _ = CargarDatosClienteAsync();
        }

        private void CargarDatosOrden()
        {
            lblOrdenId.Text = _pedido.IdVisible;
            lblMesa.Text = _pedido.MesaVisible;
            lblEstado.Text = _pedido.Estado;
            badgeEstado.BackgroundColor = Color.FromArgb(_pedido.ColorEstado);
            lblFecha.Text = _pedido.FechaLocal.ToString("dd MMM yyyy • HH:mm");
            lblTiempo.Text = _pedido.TiempoRelativo;

            lblTotal.Text = _pedido.Total.ToString("C");

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

            if (!string.IsNullOrWhiteSpace(_pedido.Comentario))
            {
                panelComentario.IsVisible = true;
                lblComentario.Text = _pedido.Comentario;
            }

            panelAcciones.IsVisible = _pedido.IsNotCancelled;
        }

        private async Task CargarDatosClienteAsync()
        {
            try
            {
                // El controlador nos da los datos del cliente
                var cliente = await _controller.ObtenerDetalleClienteAsync(_pedido.ClienteId);

                if (cliente != null)
                {
                    lblClienteNombre.Text = !string.IsNullOrEmpty(cliente.Nombre) ? cliente.Nombre : "Sin nombre registrado";
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
                Console.WriteLine($"Error en controlador al cargar cliente: {ex.Message}");
                lblClienteNombre.Text = "Error al cargar perfil";
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

            // Delegamos la actualización y notificación al controlador
            bool ok = await _controller.ActualizarEstadoOrdenAsync(_pedido.Id, _pedido.ClienteId, _pedido.IdVisible, nuevoEstado);

            if (ok)
            {
                await DisplayAlert("Actualizado", $"La orden ahora está: {nuevoEstado}", "OK");
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert("Error", "No se pudo actualizar la orden.", "OK");
                btn.IsEnabled = true;
            }
        }

        private async void OnCancelarOrden(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Cancelar Pedido",
                $"¿Seguro que quieres cancelar la orden {_pedido.IdVisible}?", "Sí, cancelar", "No");

            if (confirm)
            {
                btnCancelar.IsEnabled = false;
                // Delegamos la cancelación al controlador
                bool ok = await _controller.ActualizarEstadoOrdenAsync(_pedido.Id, _pedido.ClienteId, _pedido.IdVisible, "Cancelado");

                if (ok)
                {
                    await DisplayAlert("Cancelado", "El pedido ha sido marcado como cancelado.", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Error", "No se pudo procesar la cancelación.", "OK");
                    btnCancelar.IsEnabled = true;
                }
            }
        }
    }
}
