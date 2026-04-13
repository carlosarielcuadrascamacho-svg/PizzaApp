using PizzeriaApp.Controllers;
using PizzeriaApp.Models;
using PizzeriaApp.Services;

namespace PizzeriaApp.Views
{
    // Esta es la Vista de Alta; delega la creación del producto al AdminController
    public partial class AltaProducto : ContentPage
    {
        private string _imageBase64 = "";
        private readonly AdminController _controller;

        public AltaProducto()
        {
            InitializeComponent();
            // Inicialización del controlador con servicios especializados
            _controller = new AdminController(new ServicioReportes(), new ServicioPedidos(), new ServicioCatalogo());
        }

        private async void OnPickImageClicked(object sender, EventArgs e)
        {
            try
            {
                var photo = await MediaPicker.Default.PickPhotoAsync();
                if (photo == null) return;

                byte[] bytes;
                using (var stream = await photo.OpenReadAsync())
                using (var ms = new MemoryStream())
                {
                    await stream.CopyToAsync(ms);
                    bytes = ms.ToArray();
                }

                _imageBase64 = Convert.ToBase64String(bytes);
                imgSelected.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Fallo al seleccionar imagen: " + ex.Message, "OK");
            }
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtNombre.Text) || string.IsNullOrEmpty(txtPrecio.Text))
            {
                await DisplayAlert("Atención", "Nombre y Precio son requeridos.", "OK");
                return;
            }

            if (!decimal.TryParse(txtPrecio.Text, out decimal precio))
            {
                await DisplayAlert("Error", "Precio inválido. Solo números por favor.", "OK");
                return;
            }

            var btn = sender as Button;
            btn.IsEnabled = false;
            btn.Text = "Guardando...";

            try 
            {
                var nuevoProducto = new Producto
                {
                    Nombre = txtNombre.Text.Trim(),
                    Descripcion = txtDescripcion.Text?.Trim() ?? "",
                    Precio = precio,
                    Categoria = pickCategoria.SelectedItem?.ToString() ?? "General",
                    ImagenBase64 = _imageBase64,
                    Activo = true
                };

                // Delegamos la persistencia al controlador
                bool ok = await _controller.GuardarNuevoProductoAsync(nuevoProducto);

                if (ok)
                {
                    await DisplayAlert("¡Éxito!", "Producto añadido al catálogo.", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Error", "No se pudo guardar el producto.", "OK");
                    btn.IsEnabled = true;
                    btn.Text = "GUARDAR PRODUCTO";
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error Crítico", ex.Message, "OK");
                btn.IsEnabled = true;
                btn.Text = "GUARDAR PRODUCTO";
            }
        }
    }
}
