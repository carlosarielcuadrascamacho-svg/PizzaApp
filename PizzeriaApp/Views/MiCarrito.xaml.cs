using System;
using System.Collections.ObjectModel;
using System.Linq;
using PizzeriaApp.Models;
using PizzeriaApp.Controllers;
using PizzeriaApp.Services;

namespace PizzeriaApp.Views
{
    // El Carrito es donde ocurre la magia final; aquí el cliente revisa su pedido y lo manda a la cocina
    public partial class MiCarrito : ContentPage
    {
        private ObservableCollection<ItemCarrito> _carrito;
        private UsuarioPerfil _clienteActual;
        private DataBaseServices _dbService;

        public MiCarrito(ObservableCollection<ItemCarrito> carrito, UsuarioPerfil cliente)
        {
            InitializeComponent();
            _carrito = carrito;
            _clienteActual = cliente;
            _dbService = new DataBaseServices();
            
            // Enlazamos la colección del carrito a la lista visual de la pantalla
            ListaCarrito.ItemsSource = _carrito;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Si el que está usando la app es un Admin (Point of Sale), le mostramos el campo para asignar mesa
            // Si es un cliente normal desde su casa, ese panel se queda escondido
            PanelAdminMesa.IsVisible = _clienteActual.EsAdmin;
            
            // Calculamos cuánto va a doler la cartera
            ActualizarTotal();
        }

        // Sumamos los subtotales de todos los productos en el carrito
        private void ActualizarTotal()
        {
            decimal total = _carrito.Sum(i => i.Subtotal);
            lblTotalCosto.Text = total.ToString("C");
        }

        // Si el cliente se arrepiente de una pizza, la quitamos de la lista
        private void OnEliminarItemClicked(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var item = btn?.CommandParameter as ItemCarrito;

            if (item != null)
            {
                _carrito.Remove(item);
                ActualizarTotal();

                // Si se queda sin nada en el carrito, lo regresamos al menú automáticamente
                if (_carrito.Count == 0)
                {
                    Navigation.PopAsync();
                }
            }
        }

        // El botón decisivo: mandar la orden a Supabase
        private async void OnConfirmarOrdenClicked(object sender, EventArgs e)
        {
            if (_carrito.Count == 0) return;

            // UX: Bloqueamos el botón y ponemos un texto de espera para que no envíen el pedido doble
            var btn = sender as Button;
            btn.IsEnabled = false;
            btn.Text = "PROCESANDO...";

            try 
            {
                // Definimos el estado inicial dependiendo de quién hace el pedido
                bool isMostrador = _clienteActual.EsAdmin;
                string estadoInicial = isMostrador ? "Consumo Local" : "En preparación";
                decimal totalCosto = _carrito.Sum(i => i.Subtotal);
                
                // Si es admin, guardamos la mesa; si es cliente, se marca como Delivery por default
                string mesa = isMostrador ? (txtMesaCliente.Text?.Trim() ?? "Piso") : "Delivery";
                string comentario = txtComentario.Text?.Trim() ?? "";
                
                // Llamamos a la versión 2 del creador de pedidos que maneja transacciones y detalles de productos
                bool exito = await _dbService.CrearPedidoV2Async(_clienteActual.Id, _carrito.ToList(), totalCosto, estadoInicial, mesa, comentario);

                if (exito)
                {
                    // 1. Avisamos a los administradores que tienen chamba nueva
                    _ = NotificationService.NotificarNuevoPedidoAAdminsAsync(_dbService);

                    // 2. Le confirmamos al cliente que ya recibimos su pedido
                    _ = NotificationService.NotificarPedidoRecibidoAClienteAsync(_dbService, _clienteActual.Id);

                    await DisplayAlert("¡Excelente!", "Tu orden ha sido recibida y está en cola de cocina.", "OK");
                    
                    // Limpiamos el carrito global porque ya se convirtió en un pedido real
                    _carrito.Clear();
                    // Regresamos hasta el inicio de la navegación
                    await Navigation.PopToRootAsync();
                }
                else
                {
                    // Si falló la red o la base de datos, dejamos que el usuario intente de nuevo
                    await DisplayAlert("Error", "No pudimos procesar la orden en este momento.", "OK");
                    btn.IsEnabled = true;
                    btn.Text = "ENVIAR ORDEN";
                }
            }
            catch (Exception ex)
            {
                // Un error genérico por si truena algo en la lógica de negocio
                await DisplayAlert("Ups", "Error crítico: " + ex.Message, "OK");
                btn.IsEnabled = true;
                btn.Text = "ENVIAR ORDEN";
            }
        }
    }
}
