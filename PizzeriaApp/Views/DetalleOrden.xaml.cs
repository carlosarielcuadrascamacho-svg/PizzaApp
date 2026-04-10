using System;
using PizzeriaApp.Controllers;
using PizzeriaApp.Models;
using PizzeriaApp.Services;

namespace PizzeriaApp.Views
{
    // Esta pantalla es el "zoom" del pedido; el pizzero o el gerente ven aquí exactamente qué quiere el cliente
    public partial class DetalleOrden : ContentPage
    {
        private readonly Pedido _pedido;
        private readonly DataBaseServices _dbService;

        public DetalleOrden(Pedido pedido, DataBaseServices dbService)
        {
            InitializeComponent();
            _pedido = pedido;
            _dbService = dbService;

            // Primero pintamos lo que ya sabemos del pedido (lo que viene de la lista)
            CargarDatosOrden();
            // Luego, en segundo plano, vamos por la info del cliente (dirección, cel, etc.)
            _ = CargarDatosClienteAsync();
        }

        // Método para llenar la UI con la información básica y la lista de pizzas
        private void CargarDatosOrden()
        {
            // Datos de cabecera: Folio, Mesa y Estado actual
            lblOrdenId.Text = _pedido.IdVisible;
            lblMesa.Text = _pedido.MesaVisible;
            lblEstado.Text = _pedido.Estado;
            // Usamos el color dinámico que viene del modelo (Verde, Naranja, Rojo)
            badgeEstado.BackgroundColor = Color.FromArgb(_pedido.ColorEstado);
            lblFecha.Text = _pedido.FechaLocal.ToString("dd MMM yyyy • HH:mm");
            lblTiempo.Text = _pedido.TiempoRelativo;

            // Cuánto va a pagar
            lblTotal.Text = _pedido.Total.ToString("C");

            // Limpiamos y recreamos dinámicamente la lista de productos
            listaProductos.Children.Clear();
            foreach (var detalle in _pedido.Detalles)
            {
                // Creamos un renglón por cada producto pedido
                var itemGrid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection
                    {
                        new ColumnDefinition(new GridLength(50)), // Columna de cantidad
                        new ColumnDefinition(GridLength.Star),   // Nombre del platillo
                        new ColumnDefinition(GridLength.Auto)    // Precio subtotal
                    },
                    Padding = new Thickness(5, 4)
                };

                // Añadimos el contador (ej: x2)
                itemGrid.Add(new Label
                {
                    Text = $"x{detalle.Cantidad}",
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#FF4B3A"), // Rojo "Pizzería"
                    VerticalOptions = LayoutOptions.Center
                }, 0);

                // Nombre de la pizza o refresco
                itemGrid.Add(new Label
                {
                    Text = detalle.NombrePlatillo,
                    FontSize = 15,
                    TextColor = Color.FromArgb("#333"),
                    VerticalOptions = LayoutOptions.Center
                }, 1);

                // Subtotal individual
                itemGrid.Add(new Label
                {
                    Text = detalle.Subtotal.ToString("C"),
                    FontSize = 14,
                    TextColor = Color.FromArgb("#757575"),
                    VerticalOptions = LayoutOptions.Center
                }, 2);

                listaProductos.Children.Add(itemGrid);
            }

            // Si el cliente dejó alguna nota (ej: "sin cebolla"), la mostramos
            if (!string.IsNullOrWhiteSpace(_pedido.Comentario))
            {
                panelComentario.IsVisible = true;
                lblComentario.Text = _pedido.Comentario;
            }

            // Si el pedido ya está cancelado, ocultamos los botones de acción para que no lo muevan más
            panelAcciones.IsVisible = _pedido.IsNotCancelled;
        }

        // Función asíncrona para traer los datos de contacto del cliente desde Supabase
        private async Task CargarDatosClienteAsync()
        {
            try
            {
                // Buscamos el perfil usando el ID del cliente guardado en el pedido
                var cliente = await _dbService.ObtenerPerfilAsync(_pedido.ClienteId);

                if (cliente != null)
                {
                    // Llenamos el panel de info del cliente
                    lblClienteNombre.Text = !string.IsNullOrEmpty(cliente.Nombre) ? cliente.Nombre : "Sin nombre registrado";
                    lblClienteTelefono.Text = !string.IsNullOrEmpty(cliente.Telefono) ? cliente.Telefono : "No registrado";
                    lblClienteEmail.Text = !string.IsNullOrEmpty(cliente.Email) ? cliente.Email : "No registrado";
                    lblClienteDireccion.Text = !string.IsNullOrEmpty(cliente.Direccion) ? cliente.Direccion : "No registrada";
                }
                else
                {
                    // Por si el usuario fue borrado pero el pedido persiste
                    lblClienteNombre.Text = "Cliente no encontrado";
                    lblClienteTelefono.Text = "--";
                    lblClienteEmail.Text = "--";
                    lblClienteDireccion.Text = "--";
                }
            }
            catch (Exception ex)
            {
                // Error de red o permisos
                Console.WriteLine($"Error cargando datos del cliente: {ex.Message}");
                lblClienteNombre.Text = "Error al cargar perfil";
            }
            finally
            {
                // Quitamos el spinner y mostramos los datos (o el error)
                loadingCliente.IsRunning = false;
                loadingCliente.IsVisible = false;
                panelDatosCliente.IsVisible = true;
            }
        }

        // Lógica para avanzar el estado del pedido (Ej: De "En preparación" a "Listo para entrega")
        private async void OnCambiarEstado(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var nuevoEstado = btn?.CommandParameter as string;

            if (string.IsNullOrEmpty(nuevoEstado)) return;

            // Bloqueamos el botón para evitar que manden la misma actualización varias veces
            btn.IsEnabled = false;

            // Actualizamos en la nube
            bool ok = await _dbService.ActualizarEstadoPedidoAsync(_pedido.Id, nuevoEstado);

            if (ok)
            {
                // Paso crítico de comunicación: Avisamos al cliente vía Push que su pizza tiene novedades
                _ = NotificationService.NotificarCambioEstadoAClienteAsync(
                    _dbService, _pedido.ClienteId, nuevoEstado, _pedido.IdVisible);

                await DisplayAlert("Actualizado", $"La orden ahora está: {nuevoEstado}", "OK");
                // Regresamos a la cola de cocina
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert("Error", "No se pudo actualizar la base de datos. Revisa tu internet.", "OK");
                btn.IsEnabled = true;
            }
        }

        // Si el gerente decide cancelar la orden por algún motivo especial
        private async void OnCancelarOrden(object sender, EventArgs e)
        {
            // Siempre preguntamos dos veces antes de matar un pedido
            bool confirm = await DisplayAlert("Cancelar Pedido",
                $"¿Seguro que quieres cancelar la orden {_pedido.IdVisible}?", "Sí, cancelar", "No");

            if (confirm)
            {
                btnCancelar.IsEnabled = false;
                bool ok = await _dbService.ActualizarEstadoPedidoAsync(_pedido.Id, "Cancelado");

                if (ok)
                {
                    // También notificamos al cliente de la cancelación para que no se quede esperando su comida
                    _ = NotificationService.NotificarCambioEstadoAClienteAsync(
                        _dbService, _pedido.ClienteId, "Cancelado", _pedido.IdVisible);

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
