using System;
using System.Collections.ObjectModel;
using System.Linq;
using PizzeriaApp.Models;
using PizzeriaApp.Controllers;

namespace PizzeriaApp.Views
{
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
            
            // Asignar ItemsSource directamente
            ListaCarrito.ItemsSource = _carrito;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Lógica POS: Solo el admin asigna mesa
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
                bool isMostrador = _clienteActual.EsAdmin;
                string estadoInicial = isMostrador ? "Consumo Local" : "En preparación";
                decimal totalCosto = _carrito.Sum(i => i.Subtotal);
                
                string mesa = isMostrador ? (txtMesaCliente.Text?.Trim() ?? "Piso") : "Delivery";
                
                bool exito = await _dbService.CrearPedidoV2Async(_clienteActual.Id, _carrito.ToList(), totalCosto, estadoInicial, mesa);

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
            }
        }
    }
}
