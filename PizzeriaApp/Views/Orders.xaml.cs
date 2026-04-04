using System;
using System.Collections.ObjectModel;
using System.Linq;
using PizzeriaApp.Models;
using PizzeriaApp.Controllers;

namespace PizzeriaApp.Views
{
    public partial class Orders : ContentPage
    {
        private ObservableCollection<ItemCarrito> _carrito;
        private readonly UsuarioPerfil _clienteAsociado;
        private readonly DataBaseServices _dbService;

        private decimal _totalCalculado = 0;

        public Orders(ObservableCollection<ItemCarrito> carritoRef, UsuarioPerfil cliente)
        {
            InitializeComponent();
            _carrito = carritoRef;
            _clienteAsociado = cliente;
            _dbService = new DataBaseServices();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ActualizarVistaDeCarrito();
        }

        private void ActualizarVistaDeCarrito()
        {
            ListaCarrito.ItemsSource = null;
            ListaCarrito.ItemsSource = _carrito;

            _totalCalculado = _carrito.Sum(i => i.Subtotal);

            lblSubtotal.Text = $"{_totalCalculado:C}";
            lblTotal.Text = $"{_totalCalculado:C}";

            lblVacio.IsVisible = _carrito.Count == 0;
            btnConfirmar.IsEnabled = _carrito.Count > 0;
        }

        private async void OnConfirmarClicked(object sender, EventArgs e)
        {
            btnConfirmar.IsEnabled = false;
            btnConfirmar.Text = "Procesando...";

            bool exito = await _dbService.CrearPedidoCompletoAsync(_clienteAsociado.Id, _carrito.ToList(), _totalCalculado);

            if (exito)
            {
                await DisplayAlert("¡Orden Recibida!", "Tu pedido ha sido enviado a la cocina con éxito. En breve lo prepararemos.", "Excelente");
                
                // Vaciamos el carrito base
                _carrito.Clear();
                
                // Retornamos al menú principal del cliente
                await Navigation.PopAsync(); 
            }
            else
            {
                await DisplayAlert("Ups", "Tuvimos un problema enviando la orden.", "Reintentar");
            }

            btnConfirmar.IsEnabled = true;
            btnConfirmar.Text = "Realizar Pedido";
        }
    }
}
