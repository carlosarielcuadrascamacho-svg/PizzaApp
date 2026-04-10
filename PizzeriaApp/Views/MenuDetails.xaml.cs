using System;
using System.Collections.ObjectModel;
using PizzeriaApp.Models;

namespace PizzeriaApp.Views
{
    // Esta pantalla es el detalle de una pizza; aquí el cliente elige cuántas quiere de este tipo antes de echarlas al carrito
    public partial class MenuDetails : ContentPage
    {
        // El producto que estamos visualizando actualmente
        public Producto ProductoMostrado { get; set; }
        
        // El contador interno de piezas que el usuario quiere llevar
        private int _cantidad = 1;
        // Mantenemos la referencia al carrito principal para poder actualizarlo desde aquí
        private readonly ObservableCollection<ItemCarrito> _carritoReferencia;

        public MenuDetails(Producto producto, ObservableCollection<ItemCarrito> carritoActual)
        {
            InitializeComponent();
            ProductoMostrado = producto;
            _carritoReferencia = carritoActual;
            
            // Hacemos el binding a esta misma clase para usar las propiedades en el XAML
            BindingContext = this;
            // Calculamos el precio inicial
            ActualizarTotalUI();
        }

        // Baja la cantidad; no permitimos menos de 1 porque pues... ¿para qué entraste aquí? jaja
        private void OnDisminuirClicked(object sender, EventArgs e)
        {
            if (_cantidad > 1)
            {
                _cantidad--;
                ActualizarTotalUI();
            }
        }

        // Sube el contador; entre más pizza mejor para el negocio
        private void OnAumentarClicked(object sender, EventArgs e)
        {
            _cantidad++;
            ActualizarTotalUI();
        }

        // Este método actualiza la etiqueta de cantidad y el precio que sale en el botón de confirmación
        private void ActualizarTotalUI()
        {
            lblCantidad.Text = _cantidad.ToString();
            // Multiplicamos el precio base por las unidades elegidas
            decimal subtotal = ProductoMostrado.Precio * _cantidad;
            // Formateamos el texto del botón con el subtotal actual
            btnAgregar.Text = $"Añadir al carrito - {subtotal:C}";
        }

        // Cuando el cliente ya decidió cuántas lleva
        private async void OnConfirmarAgregarClicked(object sender, EventArgs e)
        {
            // Revisamos si ya tenía esta pizza seleccionada previamente
            var itemExistente = _carritoReferencia.FirstOrDefault(i => i.Producto.Id == ProductoMostrado.Id);

            if (itemExistente != null)
            {
                // Si ya estaba, solo le sumamos las nuevas unidades a la cantidad que ya tenía
                itemExistente.Cantidad += _cantidad;
            }
            else
            {
                // Si no estaba, agregamos la entrada completa al carrito de la sesión
                _carritoReferencia.Add(new ItemCarrito { Producto = ProductoMostrado, Cantidad = _cantidad });
            }

            // Una confirmación visual para que el cliente sepa que todo fue bien
            await DisplayAlert("Genial", $"*{ProductoMostrado.Nombre}* añadido a tu orden.", "OK");
            // Regresamos al menú principal
            await Navigation.PopAsync();
        }
    }
}
