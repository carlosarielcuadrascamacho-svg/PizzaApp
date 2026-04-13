using System;
using System.Threading.Tasks;
using PizzeriaApp.Models;
using PizzeriaApp.Controllers;
using PizzeriaApp.Services;

namespace PizzeriaApp.Views
{
    // Esta es la Vista de Gestión; delega el control del inventario al AdminController
    public partial class GestionCatalogo : ContentPage
    {
        private readonly AdminController _controller;
        private List<Producto> _inventarioCompleto = new List<Producto>();
        private string _categoriaSeleccionada = "Todos";

        public GestionCatalogo()
        {
            InitializeComponent();
            // Inicialización del controlador con servicios especializados
            _controller = new AdminController(new ServicioReportes(), new ServicioPedidos(), new ServicioCatalogo());
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarInventarioTotalAsync();
        }

        private async Task CargarInventarioTotalAsync()
        {
            try
            {
                // Iniciar Skeleton
                GridSkeleton.IsVisible = true;
                ListaInventario.IsVisible = false;
                _ = IniciarAnimacionSkeleton();

                // El controlador nos da el catálogo total (incluyendo inactivos)
                _inventarioCompleto = await _controller.ObtenerTodoElCatalogoAsync();
                
                CargarPillsCategorias();
                ActualizarLista();

                // Finalizar Skeleton
                GridSkeleton.IsVisible = false;
                ListaInventario.IsVisible = true;
                await ListaInventario.FadeTo(1, 400);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo cargar el catálogo: " + ex.Message, "OK");
            }
        }

        private async Task IniciarAnimacionSkeleton()
        {
            while (GridSkeleton.IsVisible)
            {
                await GridSkeleton.FadeTo(0.5, 800);
                await GridSkeleton.FadeTo(1.0, 800);
            }
        }

        private void CargarPillsCategorias()
        {
            if (slCategorias.Children.Count > 0) return;

            var categorias = new List<string> { "Todos" };
            var catsExistentes = _inventarioCompleto.Select(p => p.Categoria).Distinct().ToList();
            categorias.AddRange(catsExistentes);

            foreach (var cat in categorias)
            {
                var btn = CrearBotonFiltro(cat);
                slCategorias.Children.Add(btn);
            }
        }

        private Button CrearBotonFiltro(string cat)
        {
            bool esSeleccionada = cat == _categoriaSeleccionada;
            var btn = new Button
            {
                Text = cat,
                BackgroundColor = esSeleccionada ? Color.FromArgb("#FF4B3A") : Color.FromArgb("#FFFFFF"),
                TextColor = esSeleccionada ? Colors.White : Color.FromArgb("#1A1A1A"),
                CornerRadius = 20,
                Padding = new Thickness(20, 0),
                HeightRequest = 40,
                FontAttributes = FontAttributes.Bold,
                FontSize = 12,
                BorderWidth = 1,
                BorderColor = esSeleccionada ? Colors.Transparent : Color.FromArgb("#EEEEEE")
            };

            btn.Clicked += (s, e) =>
            {
                _categoriaSeleccionada = cat;
                // Actualizar estilos visuales de los botones
                foreach (var child in slCategorias.Children)
                {
                    if (child is Button b)
                    {
                        bool isSelected = b.Text == _categoriaSeleccionada;
                        b.BackgroundColor = isSelected ? Color.FromArgb("#FF4B3A") : Color.FromArgb("#FFFFFF");
                        b.TextColor = isSelected ? Colors.White : Color.FromArgb("#1A1A1A");
                        b.BorderColor = isSelected ? Colors.Transparent : Color.FromArgb("#EEEEEE");
                    }
                }
                ActualizarLista();
            };

            return btn;
        }

        private void OnBusquedaChanged(object sender, TextChangedEventArgs e)
        {
            ActualizarLista();
        }

        private void ActualizarLista()
        {
            var busqueda = txtBusqueda.Text?.ToLower() ?? "";
            
            var filtrados = _inventarioCompleto.Where(p => 
                (_categoriaSeleccionada == "Todos" || p.Categoria == _categoriaSeleccionada) &&
                (string.IsNullOrEmpty(busqueda) || p.Nombre.ToLower().Contains(busqueda) || p.Descripcion.ToLower().Contains(busqueda))
            ).ToList();

            ListaInventario.ItemsSource = null;
            ListaInventario.ItemsSource = filtrados;
        }

        private async void OnStatusToggled(object sender, ToggledEventArgs e)
        {
            if (sender is Switch sw && sw.BindingContext is Producto producto)
            {
                // Solo disparamos si el valor realmente cambió para evitar bucles
                if (producto.Activo != e.Value)
                {
                    producto.Activo = e.Value;
                    bool exito = await _controller.CambiarDisponibilidadProductoAsync(producto.Id, e.Value);
                    
                    if (!exito)
                    {
                        // Revertimos el switch si falló la base de datos
                        sw.IsToggled = !e.Value;
                        await DisplayAlert("Error", "No se pudo actualizar la disponibilidad", "OK");
                    }
                }
            }
        }

        private async void OnAgregarProductoClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new AltaProducto());
        }

        private async void OnEditarProductoTapped(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var productoSeleccionado = btn?.CommandParameter as Producto;
            if (productoSeleccionado != null)
            {
                await Navigation.PushAsync(new EditarProducto(productoSeleccionado));
            }
        }
    }
}
