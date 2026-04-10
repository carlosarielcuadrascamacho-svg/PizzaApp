using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Media;
using PizzeriaApp.Controllers;
using PizzeriaApp.Models;
using PizzeriaApp.Converters;

namespace PizzeriaApp.Views
{
    // Esta pantalla nos sirve para corregir precios, nombres o fotos de las pizzas que ya tenemos dadas de alta
    public partial class EditarProducto : ContentPage
    {
        // Guardamos el Base64 de la imagen (sea la nueva o la que ya tenía)
        private string _imageBase64 = "";
        private readonly DataBaseServices _dbService;
        private readonly Producto _productoOriginal;

        public EditarProducto(Producto producto)
        {
            InitializeComponent();
            _dbService = new DataBaseServices();
            // Guardamos la referencia del producto que queremos editar
            _productoOriginal = producto;
            
            // Llenamos los campos de la pantalla con la info actual
            CargarDatosProducto();
        }

        // Método para volcar la data del objeto Producto a los controles de la UI
        private void CargarDatosProducto()
        {
            txtNombre.Text = _productoOriginal.Nombre;
            txtDescripcion.Text = _productoOriginal.Descripcion;
            txtPrecio.Text = _productoOriginal.Precio.ToString("0.00");
            pickCategoria.SelectedItem = _productoOriginal.Categoria;
            swActivo.IsToggled = _productoOriginal.Activo;
            _imageBase64 = _productoOriginal.ImagenBase64;

            // Si el producto ya tiene foto, la mostramos usando nuestro convertidor de Base64
            if (!string.IsNullOrEmpty(_imageBase64))
            {
                var converter = new Base64ToImageConverter();
                imgSelected.Source = (ImageSource)converter.Convert(_imageBase64, typeof(ImageSource), null, null);
            }
        }

        // Reutilizamos la lógica de selección de imagen, por si quieren cambiar la foto de la pizza
        private async void OnPickImageClicked(object sender, EventArgs e)
        {
            try
            {
                var photo = await MediaPicker.Default.PickPhotoAsync();
                if (photo == null) return;

                // Proceso estándar: Leer bytes -> Convertir a Base64 -> Mostrar previa
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
                await DisplayAlert("Error", "Fallo al seleccionar nueva imagen: " + ex.Message, "OK");
            }
        }

        // Cuando el admin termina de editar y le da al botón de guardar
        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            // Validaciones de rigor: nombre y precio no pueden faltar
            if (string.IsNullOrEmpty(txtNombre.Text) || string.IsNullOrEmpty(txtPrecio.Text))
            {
                await DisplayAlert("Atención", "Nombre y Precio son requeridos.", "OK");
                return;
            }

            if (!decimal.TryParse(txtPrecio.Text, out decimal precio))
            {
                await DisplayAlert("Error", "El precio debe ser un número válido.", "OK");
                return;
            }

            // UX: Desactivamos el botón para evitar clics dobles que creen basura en la BD
            btnGuardar.IsEnabled = false;
            btnGuardar.Text = "Guardando...";

            try 
            {
                // Actualizamos el objeto original con la nueva información del formulario
                _productoOriginal.Nombre = txtNombre.Text.Trim();
                _productoOriginal.Descripcion = txtDescripcion.Text?.Trim() ?? "";
                _productoOriginal.Precio = precio;
                _productoOriginal.Categoria = pickCategoria.SelectedItem?.ToString() ?? "General";
                _productoOriginal.ImagenBase64 = _imageBase64;
                _productoOriginal.Activo = swActivo.IsToggled;

                // Mandamos la actualización a Supabase
                bool ok = await _dbService.ActualizarProductoAsync(_productoOriginal);

                if (ok)
                {
                    // Todo chido, avisamos y cerramos la pantalla
                    await DisplayAlert("¡Éxito!", "Producto actualizado correctamente.", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    // Si algo falló, dejamos que el usuario intente de nuevo sin perder lo que escribió
                    await DisplayAlert("Error", "No se pudo actualizar en Supabase. Revisa tu conexión.", "OK");
                    btnGuardar.IsEnabled = true;
                    btnGuardar.Text = "GUARDAR CAMBIOS";
                }
            }
            catch (Exception ex)
            {
                // Un log de emergencia por si el error es algo de código o mapeo
                await DisplayAlert("Error Crítico", ex.Message, "OK");
                btnGuardar.IsEnabled = true;
                btnGuardar.Text = "GUARDAR CAMBIOS";
            }
        }
    }
}
