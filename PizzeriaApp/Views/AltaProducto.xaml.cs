using PizzeriaApp.Controllers;
using PizzeriaApp.Models;

namespace PizzeriaApp.Views
{
    public partial class AltaProducto : ContentPage
    {
        private string _imageBase64 = "";
        private DataBaseServices _dbService;

        public AltaProducto()
        {
            InitializeComponent();
            _dbService = new DataBaseServices();
        }

        private async void OnPickImageClicked(object sender, EventArgs e)
        {
            try
            {
                var photo = await MediaPicker.Default.PickPhotoAsync();
                if (photo == null) return;

                var stream = await photo.OpenReadAsync();
                imgSelected.Source = ImageSource.FromStream(() => stream);

                byte[] bytes;
                using (var ms = new MemoryStream())
                {
                    var streamFull = await photo.OpenReadAsync();
                    await streamFull.CopyToAsync(ms);
                    bytes = ms.ToArray();
                }
                
                _imageBase64 = Convert.ToBase64String(bytes);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Fallo al seleccionar imagen: " + ex.Message, "OK");
            }
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            // Validaciones básicas de formulario POS
            if (string.IsNullOrEmpty(txtNombre.Text) || string.IsNullOrEmpty(txtPrecio.Text))
            {
                await DisplayAlert("Atención", "Nombre y Precio son requeridos.", "OK");
                return;
            }

            if (!decimal.TryParse(txtPrecio.Text, out decimal precio))
            {
                await DisplayAlert("Error", "Precio inválido.", "OK");
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
                    ImagenBase64 = _imageBase64, // El string que capturamos en el evento PickImage
                    Activo = true
                };

                bool ok = await _dbService.InsertarProductoAsync(nuevoProducto);

                if (ok)
                {
                    await DisplayAlert("¡Éxito!", "Producto añadido al catálogo.", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Error", "No se pudo guardar en Supabase.", "OK");
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
