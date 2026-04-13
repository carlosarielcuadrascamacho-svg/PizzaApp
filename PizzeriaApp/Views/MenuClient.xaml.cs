using PizzeriaApp.Models;
using PizzeriaApp.Controllers;
using PizzeriaApp.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace PizzeriaApp.Views
{
    // Esta es la Vista del catálogo; delega la obtención de productos al OrderController
    public partial class MenuClient : ContentPage
    {
        private readonly UsuarioPerfil _clienteActual;
        private readonly OrderController _controller;
        private readonly ServicioCatalogo _servicioCatalogo;

        // El carrito vive en memoria mientras el usuario navega
        private ObservableCollection<ItemCarrito> _carrito;
        private List<Producto> _todosLosProductos = new List<Producto>();
        private string _categoriaSeleccionada = "Todos";

        public MenuClient(UsuarioPerfil cliente)
        {
            InitializeComponent();
            _clienteActual = cliente;
            _servicioCatalogo = new ServicioCatalogo();
            _controller = new OrderController(_servicioCatalogo, new ServicioPedidos());
            _carrito = new ObservableCollection<ItemCarrito>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            // Iniciamos el Skeleton
            GridSkeleton.IsVisible = true;
            ListaProductos.IsVisible = false;
            _ = IniciarAnimacionSkeletion();

            await CargarMenuAsync();
            
            // NUEVO: Intentar cargar carrito persistido (AISLADO POR USUARIO)
            CargarCarritoPersistido();

            ActualizarCarritoUI();
            
            ListaProductos.SelectedItem = null;
        }

        private void CargarCarritoPersistido()
        {
            try
            {
                // Key aislada por usuario
                string key = $"cart_storage_{_clienteActual.Id}";
                string json = Microsoft.Maui.Storage.Preferences.Get(key, string.Empty);
                if (string.IsNullOrEmpty(json)) return;

                var savedItems = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CartSaveItem>>(json);
                if (savedItems == null || !savedItems.Any()) return;

                _carrito.Clear();
                foreach (var sItem in savedItems)
                {
                    var producto = _todosLosProductos.FirstOrDefault(p => p.Id == sItem.ProductId);
                    if (producto != null)
                    {
                        _carrito.Add(new ItemCarrito { Producto = producto, Cantidad = sItem.Quantity });
                    }
                }
            }
            catch { /* Silencioso en caso de error de parsing */ }
        }

        private void GuardarCarritoActual()
        {
            GuardarPersistencia(_clienteActual.Id, _carrito);
        }

        // Método estático para ser llamado desde cualquier parte (MenuClient o MiCarrito)
        public static void GuardarPersistencia(string userId, IEnumerable<ItemCarrito> carrito)
        {
            try
            {
                string key = $"cart_storage_{userId}";

                if (carrito == null || !carrito.Any())
                {
                    Microsoft.Maui.Storage.Preferences.Remove(key);
                    return;
                }

                var itemsParaGuardar = carrito.Select(i => new CartSaveItem 
                { 
                    ProductId = i.Producto.Id, 
                    Quantity = i.Cantidad 
                }).ToList();

                string json = Newtonsoft.Json.JsonConvert.SerializeObject(itemsParaGuardar);
                Microsoft.Maui.Storage.Preferences.Set(key, json);
            }
            catch { }
        }

        // Método estático para ser llamado desde MiCarrito tras ordenar
        public static void LimpiarPersistencia(string userId)
        {
            Microsoft.Maui.Storage.Preferences.Remove($"cart_storage_{userId}");
        }

        private async Task IniciarAnimacionSkeletion()
        {
            while (GridSkeleton.IsVisible)
            {
                await GridSkeleton.FadeTo(0.5, 800);
                await GridSkeleton.FadeTo(1.0, 800);
            }
        }

        private void ActualizarCarritoUI()
        {
            if (_carrito == null) return;

            int totalArticulos = _carrito.Sum(i => i.Cantidad);
            decimal totalCosto = _carrito.Sum(i => i.Subtotal);

            lblCartItems.Text = totalArticulos.ToString();
            lblCartTotal.Text = totalCosto.ToString("C");
            
            ContenedorCarrito.IsVisible = totalArticulos > 0;

            // Cada vez que la UI se actualiza, el estado del carrito ha cambiado, guardamos.
            GuardarCarritoActual();
        }

        private async Task CargarMenuAsync()
        {
            // Cargamos de DB
            _todosLosProductos = await _servicioCatalogo.ObtenerCatalogoCompletoAsync();
            
            CargarCategorias();
            FiltrarProductos();

            // Ocultamos skeleton y mostramos lista real con una transición suave
            GridSkeleton.IsVisible = false;
            ListaProductos.IsVisible = true;
            await ListaProductos.FadeTo(1, 400);
        }

        private void CargarCategorias()
        {
            if (slCategorias.Children.Count > 0) return;

            var categorias = new List<string> { "Todos" };
            var categoriasProductos = _todosLosProductos
                .Where(p => p.Activo)
                .Select(p => p.Categoria)
                .Distinct();

            categorias.AddRange(categoriasProductos);

            foreach (var cat in categorias)
            {
                var btn = CrearBotonCategoria(cat);
                slCategorias.Children.Add(btn);
            }
        }

        private string ObtenerIconoCategoria(string cat)
        {
            return cat.ToLower() switch
            {
                "todos" => "🌎",
                "pizzas" => "🍕",
                "bebidas" => "🥤",
                "entradas" => "🥗",
                "postres" => "🍰",
                _ => "🍴"
            };
        }

        private Button CrearBotonCategoria(string cat)
        {
            bool esSeleccionada = cat == _categoriaSeleccionada;
            string icono = ObtenerIconoCategoria(cat);
            
            var btn = new Button
            {
                Text = $"{icono} {cat}",
                BackgroundColor = esSeleccionada ? Color.FromArgb("#FF4B3A") : Color.FromArgb("#F2F2F2"),
                TextColor = esSeleccionada ? Colors.White : Color.FromArgb("#1A1A1A"),
                CornerRadius = 22,
                Padding = new Thickness(25, 0),
                HeightRequest = 44,
                FontAttributes = FontAttributes.Bold,
                FontSize = 13
            };

            btn.Clicked += (s, e) =>
            {
                _categoriaSeleccionada = cat;
                // Actualizar estilos de todos los botones
                foreach (var child in slCategorias.Children)
                {
                    if (child is Button b)
                    {
                        bool isSelected = b.Text.Contains(_categoriaSeleccionada);
                        b.BackgroundColor = isSelected ? Color.FromArgb("#FF4B3A") : Color.FromArgb("#F2F2F2");
                        b.TextColor = isSelected ? Colors.White : Color.FromArgb("#1A1A1A");
                    }
                }
                FiltrarProductos();
            };

            return btn;
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            FiltrarProductos();
        }

        private void FiltrarProductos()
        {
            var texto = txtBusqueda.Text?.ToLower() ?? "";
            
            var filtrados = _todosLosProductos.Where(p => 
                p.Activo &&
                (_categoriaSeleccionada == "Todos" || p.Categoria == _categoriaSeleccionada) &&
                (string.IsNullOrEmpty(texto) || p.Nombre.ToLower().Contains(texto) || p.Descripcion.ToLower().Contains(texto))
            ).ToList();

            ListaProductos.ItemsSource = null;
            ListaProductos.ItemsSource = filtrados;
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

    public class CartSaveItem
    {
        public string ProductId { get; set; }
        public int Quantity { get; set; }
    }
}