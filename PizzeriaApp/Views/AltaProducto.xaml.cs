using PizzeriaApp.Models;
using PizzeriaApp.Controllers;

namespace PizzeriaApp.Views
{
    public partial class AltaProducto : ContentPage
    {
        private byte[] _imageBytes;
        private string _imageExt;
        private readonly DataBaseServices _dbService;

        public AltaProducto()
        {
            InitializeComponent();
            _dbService = new DataBaseServices();
        }

        private async void OnAñadirFotoClicked(object sender, EventArgs e)
        {
            var accion = await DisplayActionSheet("Añadir Imagen", "Cancelar", null, "Tomar Foto", "Elegir de Galería");

            try
            {
                FileResult photo = null;

                if (accion == "Tomar Foto")
                {
                    if (MediaPicker.Default.IsCaptureSupported)
                    {
                        photo = await MediaPicker.Default.CapturePhotoAsync();
                    }
                }
                else if (accion == "Elegir de Galería")
                {
                    photo = await MediaPicker.Default.PickPhotoAsync();
                }

                if (photo != null)
                {
                    using (var stream = await photo.OpenReadAsync())
                    using (var memoryStream = new MemoryStream())
                    {
                        await stream.CopyToAsync(memoryStream);
                        _imageBytes = memoryStream.ToArray();
                    }

                    _imageExt = Path.GetExtension(photo.FileName);

                    // Forzar dibujado en el UI Thread
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        imgPreview.Source = ImageSource.FromStream(() => new MemoryStream(_imageBytes));
                        imgPreview.IsVisible = true;
                    });
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Problema con la cámara/galería: {ex.Message}", "Ok");
            }
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text) || string.IsNullOrWhiteSpace(txtPrecio.Text))
            {
                await DisplayAlert("Error", "Faltan datos obligatorios.", "OK");
                return;
            }

            if (!decimal.TryParse(txtPrecio.Text, out decimal precio))
            {
                await DisplayAlert("Error", "El precio no es válido.", "OK");
                return;
            }

            btnGuardar.IsEnabled = false;
            btnGuardar.Text = "Guardando...";

            string publicUrl = "https://via.placeholder.com/400x180.png?text=Sin+Imagen";

            if (_imageBytes != null)
            {
                // Codificar la imagen directamente a Base64 por requerimiento
                publicUrl = Convert.ToBase64String(_imageBytes);
            }

            var nuevoProducto = new Producto
            {
                Nombre = txtNombre.Text.Trim(),
                Descripcion = txtDescripcion.Text?.Trim() ?? "",
                Precio = precio,
                ImagenBase64 = publicUrl,
                Categoria = "Pizzas", // Default
                Activo = true
            };

            bool exito = await _dbService.InsertarProductoAsync(nuevoProducto);

            if (exito)
            {
                await DisplayAlert("Éxito", "El producto se ha guardado correctamente.", "OK");
                // Regresamos a la pantalla anterior
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert("Error", "No se pudo guardar el producto.", "OK");
            }

            btnGuardar.Text = "Guardar Producto";
            btnGuardar.IsEnabled = true;
        }
    }
}
