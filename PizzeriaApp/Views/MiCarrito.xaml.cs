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

            bool carritoVacio = _carrito.Count == 0;
            emptyState.IsVisible = carritoVacio;
            ListaCarrito.IsVisible = !carritoVacio;

            // SINCRONIZACIÓN: Guardar el estado actual en el almacenamiento local
            MenuClient.GuardarPersistencia(_clienteActual.Id, _carrito);
        }

        private async void OnRegresarMenuClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private void OnAumentarCantidadClicked(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var item = btn?.CommandParameter as ItemCarrito;

            if (item != null)
            {
                item.Cantidad += 1;
                ActualizarTotal();
            }
        }

        private void OnDisminuirCantidadClicked(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var item = btn?.CommandParameter as ItemCarrito;

            if (item != null)
            {
                if (item.Cantidad > 1)
                {
                    item.Cantidad -= 1;
                }
                else
                {
                    // Si llega a 0 (estaba en 1 y bajó), se elimina automáticamente
                    _carrito.Remove(item);
                }
                ActualizarTotal();
            }
        }

        private void OnEliminarItemClicked(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var item = (ItemCarrito)btn?.BindingContext; // Cambiamos a BindingContext para mayor seguridad

            if (item != null)
            {
                _carrito.Remove(item);
                ActualizarTotal();
            }
        }

        private async void OnConfirmarOrdenClicked(object sender, EventArgs e)
        {
            if (_carrito.Count == 0) 
            {
                await DisplayAlert("Carrito vacío", "Agrega al menos una pizza antes de pedir.", "OK");
                return;
            }

            var btn = sender as Button;
            btn.IsEnabled = false;
            string originalText = btn.Text;
            btn.Text = "PROCESANDO...";

            try 
            {
                // Si no es admin, la mesa es por defecto "Mesa 1-Cliente" o similar, o capturar de txtMesaCliente si está habilitado
                string mesa = txtMesaCliente.Text?.Trim() ?? "Cliente";
                string comentario = txtComentario.Text?.Trim() ?? "";
                
                bool exito = await _controller.ProcesarPedidoAsync(_clienteActual, _carrito, mesa, comentario);

                if (exito)
                {
                    // 1. Limpiamos el carrito en memoria
                    _carrito.Clear();

                    // 2. NUEVO: Limpiamos la persistencia local de este usuario
                    MenuClient.LimpiarPersistencia(_clienteActual.Id);
                    
                    if (_clienteActual.EsAdmin)
                    {
                        // Si es admin, no mostramos pantalla festiva, volvemos al inicio
                        await DisplayAlert("Orden Exitosa", "La orden se ha enviado a cocina.", "OK");
                        await Navigation.PopToRootAsync();
                    }
                    else
                    {
                        // Si es cliente, redirigir a la nueva pantalla de éxito festiva
                        await Navigation.PushAsync(new SuccessOrder(_clienteActual));
                        
                        // Remover esta página del stack para evitar volver al carrito vacío
                        var cartPage = Navigation.NavigationStack.FirstOrDefault(p => p is MiCarrito);
                        if (cartPage != null) Navigation.RemovePage(cartPage);
                    }
                }
                else
                {
                    await DisplayAlert("Error", "No pudimos procesar la orden en este momento.", "OK");
                    btn.IsEnabled = true;
                    btn.Text = originalText;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ups", "Error crítico: " + ex.Message, "OK");
                btn.IsEnabled = true;
                btn.Text = originalText;
            }
        }
    }
}
