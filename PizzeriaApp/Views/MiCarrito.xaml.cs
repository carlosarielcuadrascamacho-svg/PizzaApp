using System;
using System.Collections.ObjectModel;
using System.Linq;
using PizzeriaApp.Models;
using PizzeriaApp.Controllers;
using PizzeriaApp.Services;

namespace PizzeriaApp.Views
{
    // Esta es la Vista del Carrito; delega la creación del pedido al OrderController
    public partial class MiCarrito : ContentPage
    {
        private ObservableCollection<ItemCarrito> _carrito;
        private UsuarioPerfil _clienteActual;
        private OrderController _controller;

        public MiCarrito(ObservableCollection<ItemCarrito> carrito, UsuarioPerfil cliente)
        {
            InitializeComponent();
            _carrito = carrito;
            _clienteActual = cliente;
            // Inicializamos el controlador pasándole el servicio de datos
            _controller = new OrderController(new DataBaseServices());
            
            ListaCarrito.ItemsSource = _carrito;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            PanelAdminMesa.IsVisible = _clienteActual.EsAdmin;
            ActualizarTotal();
        }

        private void ActualizarTotal()
        {
            decimal total = _carrito.Sum(i => i.Subtotal);
            lblTotalCosto.Text = total.ToString("C");
        }

        private void OnEliminarItemClicked(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var item = btn?.CommandParameter as ItemCarrito;

            if (item != null)
            {
                _carrito.Remove(item);
                ActualizarTotal();

                if (_carrito.Count == 0)
                {
                    Navigation.PopAsync();
                }
            }
        }

        private async void OnConfirmarOrdenClicked(object sender, EventArgs e)
        {
            if (_carrito.Count == 0) return;

            var btn = sender as Button;
            btn.IsEnabled = false;
            btn.Text = "PROCESANDO...";

            try 
            {
                string mesa = _clienteActual.EsAdmin ? (txtMesaCliente.Text?.Trim() ?? "Piso") : "Delivery";
                string comentario = txtComentario.Text?.Trim() ?? "";
                
                // El controlador orquesta la creación del pedido y las notificaciones
                bool exito = await _controller.ProcesarPedidoAsync(_clienteActual, _carrito, mesa, comentario);

                if (exito)
                {
                    await DisplayAlert("¡Excelente!", "Tu orden ha sido recibida y está en cola de cocina.", "OK");
                    _carrito.Clear();
                    await Navigation.PopToRootAsync();
                }
                else
                {
                    await DisplayAlert("Error", "No pudimos procesar la orden en este momento.", "OK");
                    btn.IsEnabled = true;
                    btn.Text = "ENVIAR ORDEN";
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ups", "Error crítico: " + ex.Message, "OK");
                btn.IsEnabled = true;
                btn.Text = "ENVIAR ORDEN";
            }
        }
    }
}
