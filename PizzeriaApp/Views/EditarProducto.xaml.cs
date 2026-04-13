using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Media;
using PizzeriaApp.Controllers;
using PizzeriaApp.Models;
using PizzeriaApp.Converters;
using PizzeriaApp.Services;

namespace PizzeriaApp.Views
{
    // Esta es la Vista de Edición; delega la actualización al AdminController
    public partial class EditarProducto : ContentPage
    {
        private string _imageBase64 = "";
        private readonly AdminController _controller;
        private readonly Producto _productoOriginal;

        public EditarProducto(Producto producto)
        {
            InitializeComponent();
            // Inicialización del controlador administrativo con servicios especializados
            _controller = new AdminController(new ServicioReportes(), new ServicioPedidos(), new ServicioCatalogo());
            _productoOriginal = producto;
            
            CargarDatosProducto();
        }

        private void CargarDatosProducto()
        {
            txtNombre.Text = _productoOriginal.Nombre;
            txtDescripcion.Text = _productoOriginal.Descripcion;
            txtPrecio.Text = _productoOriginal.Precio.ToString("0.00");
            pickCategoria.SelectedItem = _productoOriginal.Categoria;
            swActivo.IsToggled = _productoOriginal.Activo;
            _imageBase64 = _productoOriginal.ImagenBase64;

            if (!string.IsNullOrEmpty(_imageBase64))
            {
                var converter = new Base64ToImageConverter();
                imgSelected.Source = (ImageSource)converter.Convert(_imageBase64, typeof(ImageSource), null, null);
            }
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
                await DisplayAlert("Error", "Fallo al seleccionar nueva imagen: " + ex.Message, "OK");
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
                await DisplayAlert("Error", "El precio debe ser un número válido.", "OK");
                return;
            }

            btnGuardar.IsEnabled = false;
            btnGuardar.Text = "Guardando...";

            try 
            {
                _productoOriginal.Nombre = txtNombre.Text.Trim();
                _productoOriginal.Descripcion = txtDescripcion.Text?.Trim() ?? "";
                _productoOriginal.Precio = precio;
                _productoOriginal.Categoria = pickCategoria.SelectedItem?.ToString() ?? "General";
                _productoOriginal.ImagenBase64 = _imageBase64;
                _productoOriginal.Activo = swActivo.IsToggled;

                // Delegamos la actualización al controlador
                bool ok = await _controller.ActualizarProductoAsync(_productoOriginal);

                if (ok)
                {
                    await DisplayAlert("¡Éxito!", "Producto actualizado correctamente.", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Error", "No se pudo actualizar el producto.", "OK");
                    btnGuardar.IsEnabled = true;
                    btnGuardar.Text = "GUARDAR CAMBIOS";
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error Crítico", ex.Message, "OK");
                btnGuardar.IsEnabled = true;
                btnGuardar.Text = "GUARDAR CAMBIOS";
            }
        }
    }
}
