using PizzeriaApp.Models;
using PizzeriaApp.Controllers;
using System.Collections.ObjectModel;
using System.Linq;

namespace PizzeriaApp.Views
{
    // Esta es la "cara" de la pizzería para el cliente; aquí es donde elige qué va a cenar hoy
    public partial class MenuClient : ContentPage
    {
        private readonly UsuarioPerfil _clienteActual;
        private readonly DataBaseServices _dbService;

        // El carrito vive en memoria mientras el usuario navega; usamos ObservableCollection para que la UI se entere de los cambios
        private ObservableCollection<ItemCarrito> _carrito;
        private List<Producto> _catalogoCompleto;

        public MenuClient(UsuarioPerfil cliente)
        {
            InitializeComponent();
            _clienteActual = cliente;
            _dbService = new DataBaseServices();
            _carrito = new ObservableCollection<ItemCarrito>();
        }

        // Cada vez que el cliente regresa al menú, revisamos si hay pizzas nuevas y refrescamos el estado del carrito
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            // Cargamos los botones de categorías (Pizzas, Bebidas, etc.)
            CargarFiltrosCategorias();
            
            // Vamos por los productos a Supabase
            await CargarCatalogoAsync();
            // Actualizamos el resumen que sale arriba o abajo en la pantalla (total de artículos y precio)
            ActualizarCarritoUI();
            
            // Deseleccionamos cualquier cosa para que la lista esté limpia
            ListaProductos.SelectedItem = null;
        }

        // Este método mantiene al día el resumen de la compra
        private void ActualizarCarritoUI()
        {
            int totalArticulos = _carrito.Sum(i => i.Cantidad);
            decimal totalCosto = _carrito.Sum(i => i.Subtotal);

            // Pintamos los totales en los labels de la pantalla
            lblCartItems.Text = $"{totalArticulos} artículos";
            lblCartTotal.Text = totalCosto.ToString("C");
            
            // Si el carrito está vacío, escondemos la barrita de "Confirmar Orden" para que no estorbe
            ContenedorCarrito.IsVisible = totalArticulos > 0;
        }

        // Jalamos solo los productos que el admin marcó como "Activos"
        private async Task CargarCatalogoAsync()
        {
            var productos = await _dbService.ObtenerProductosActivosAsync();
            _catalogoCompleto = productos;
            // Mostramos todo el catálogo al inicio
            ListaProductos.ItemsSource = _catalogoCompleto;
        }

        // Generamos dinámicamente los botones de filtrado para que la UI se vea moderna y limpia
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
                
                // Programamos el filtro: al picar, usamos LINQ para recortar la lista que mostramos
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

        // Si el cliente quiere ver la descripción o ingredientes detallados de una pizza
        private async void OnProductoSeleccionado(object sender, SelectionChangedEventArgs e)
        {
            // Evitamos errores si por algo se dispara la selección con algo nulo
            if (e.CurrentSelection.FirstOrDefault() is Producto productoSeleccionado)
            {
                // Mandamos a la pantalla de detalles compartiendo la referencia del carrito
                await Navigation.PushAsync(new MenuDetails(productoSeleccionado, _carrito));
            }
        }

        // El atajo rápido desde la lista principal: agregar una pizza directo al carrito
        private void OnAgregarCarritoClicked(object sender, EventArgs e)
        {
            var boton = sender as Button;
            var producto = boton?.BindingContext as Producto;

            if (producto != null)
            {
                // Buscamos si ya tiene esa pizza en el carrito para solo sumar 1 a la cantidad
                var itemExistente = _carrito.FirstOrDefault(i => i.Producto.Id == producto.Id);
                if (itemExistente != null)
                {
                    itemExistente.Cantidad += 1;
                }
                else
                {
                    // Si es nueva en esta sesión, agregamos el item completo
                    _carrito.Add(new ItemCarrito { Producto = producto, Cantidad = 1 });
                }
                // Refrescamos los numeritos de la UI
                ActualizarCarritoUI();
            }
        }

        // Cuando el cliente ya tiene hambre y decide ir a pagar/confirmar su pedido
        private async void OnConfirmarOrdenClicked(object sender, EventArgs e)
        {
            if (_carrito.Count == 0) return; // Seguridad: no navegar si no compró nada
            // Pasamos el carrito y el perfil del cliente para el proceso de checkout
            await Navigation.PushAsync(new MiCarrito(_carrito, _clienteActual));
        }
    }
}