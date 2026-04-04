using System;
using System.Collections.ObjectModel;
using PizzeriaApp.Models;

namespace PizzeriaApp.Views
{
    public partial class MenuDetails : ContentPage
    {
        public Producto ProductoMostrado { get; set; }
        
        private int _cantidad = 1;
        private readonly ObservableCollection<ItemCarrito> _carritoReferencia;

        public MenuDetails(Producto producto, ObservableCollection<ItemCarrito> carritoActual)
        {
            InitializeComponent();
            ProductoMostrado = producto;
            _carritoReferencia = carritoActual;
            
            BindingContext = this;
            ActualizarTotalUI();
        }

        private void OnDisminuirClicked(object sender, EventArgs e)
        {
            if (_cantidad > 1)
            {
                _cantidad--;
                ActualizarTotalUI();
            }
        }

        private void OnAumentarClicked(object sender, EventArgs e)
        {
            _cantidad++;
            ActualizarTotalUI();
        }

        private void ActualizarTotalUI()
        {
            lblCantidad.Text = _cantidad.ToString();
            decimal subtotal = ProductoMostrado.Precio * _cantidad;
            btnAgregar.Text = $"Añadir al carrito - {subtotal:C}";
        }

        private async void OnConfirmarAgregarClicked(object sender, EventArgs e)
        {
            // Verificamos si ya existe en el carrito
            var itemExistente = _carritoReferencia.FirstOrDefault(i => i.Producto.Id == ProductoMostrado.Id);

            if (itemExistente != null)
            {
                itemExistente.Cantidad += _cantidad;
            }
            else
            {
                _carritoReferencia.Add(new ItemCarrito { Producto = ProductoMostrado, Cantidad = _cantidad });
            }

            // Animación suave de despedida o mensaje
            await DisplayAlert("Genial", $"*{ProductoMostrado.Nombre}* añadido a tu orden.", "OK");
            await Navigation.PopAsync();
        }
    }
}
