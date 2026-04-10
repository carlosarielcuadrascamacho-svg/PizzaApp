using PizzeriaApp.Models;
using PizzeriaApp.Controllers;
using System.Collections.ObjectModel;
using System.Linq;

namespace PizzeriaApp.Views
{
    public partial class MenuClient : ContentPage
    {
        private readonly UsuarioPerfil _clienteActual;
        private readonly DataBaseServices _dbService;

        // Estructuras de datos para manejar el estado visual
        private ObservableCollection<ItemCarrito> _carrito;
        private List<Producto> _catalogoCompleto;

        public MenuClient(UsuarioPerfil cliente)
        {
            InitializeComponent();
            _clienteActual = cliente;
            _dbService = new DataBaseServices();
            _carrito = new ObservableCollection<ItemCarrito>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            CargarFiltrosCategorias();
            
            await CargarCatalogoAsync();
            ActualizarCarritoUI();
            
            ListaProductos.SelectedItem = null;
        }

        private void ActualizarCarritoUI()
        {
            int totalArticulos = _carrito.Sum(i => i.Cantidad);
            decimal totalCosto = _carrito.Sum(i => i.Subtotal);

            lblCartItems.Text = $"{totalArticulos} artículos";
            lblCartTotal.Text = totalCosto.ToString("C");
            
            ContenedorCarrito.IsVisible = totalArticulos > 0;
        }

        private async Task CargarCatalogoAsync()
        {
            var productos = await _dbService.ObtenerProductosActivosAsync();
            _catalogoCompleto = productos;
            ListaProductos.ItemsSource = _catalogoCompleto;
        }

        private void CargarFiltrosCategorias()
        {
            slCategorias.Children.Clear();
            var categorias = new List<string> { "Todas", "Pizzas", "Bebidas"};

            foreach (var cat in categorias)
            {
                var btn = new Button
                {
                    Text = cat,
                    BackgroundColor = Colors.White,
                    TextColor = Color.FromArgb("#333"),
                    BorderColor = Color.FromArgb("#E0E0E0"),
                    BorderWidth = 1,
                    Padding = new Thickness(15, 5),
                    CornerRadius = 20,
                    FontAttributes = FontAttributes.Bold
                };
                
                btn.Clicked += (sender, e) => 
                {
                    if(cat == "Todas")
                        ListaProductos.ItemsSource = _catalogoCompleto;
                    else
                        ListaProductos.ItemsSource = _catalogoCompleto.Where(p => p.Categoria == cat).ToList();
                };

                slCategorias.Children.Add(btn);
            }
        }

        private async void OnProductoSeleccionado(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Producto productoSeleccionado)
            {
                await Navigation.PushAsync(new MenuDetails(productoSeleccionado, _carrito));
            }
        }

        private void OnAgregarCarritoClicked(object sender, EventArgs e)
        {
            var boton = sender as Button;
            var producto = boton?.BindingContext as Producto;

            if (producto != null)
            {
                var itemExistente = _carrito.FirstOrDefault(i => i.Producto.Id == producto.Id);
                if (itemExistente != null)
                {
                    itemExistente.Cantidad += 1;
                }
                else
                {
                    _carrito.Add(new ItemCarrito { Producto = producto, Cantidad = 1 });
                }
                ActualizarCarritoUI();
            }
        }

        private async void OnConfirmarOrdenClicked(object sender, EventArgs e)
        {
            if (_carrito.Count == 0) return;
            await Navigation.PushAsync(new MiCarrito(_carrito, _clienteActual));
        }
    }
}