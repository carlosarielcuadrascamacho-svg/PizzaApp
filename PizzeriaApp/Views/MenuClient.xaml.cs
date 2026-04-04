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

            int totalArticulos = _carrito.Sum(i => i.Cantidad);
            tbCart.Text = $"🛒 Carrito ({totalArticulos})";
            
            ListaProductos.SelectedItem = null;
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
            var categorias = new List<string> { "Todas", "Pizzas", "Bebidas", "Postres", "Complementos" };

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

        private async void OnVerCarritoClicked(object sender, EventArgs e)
        {
            if (_carrito.Count == 0)
            {
                await DisplayAlert("Carrito", "Agrega al menos un producto para proceder al pago.", "OK");
                return;
            }
            await Navigation.PushAsync(new Orders(_carrito, _clienteActual));
        }

        private async void OnIrPerfilClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PerfilCliente(_clienteActual));
        }

        private async void OnIrHistorialClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new HistorialCliente(_clienteActual.Id));
        }
    }
}