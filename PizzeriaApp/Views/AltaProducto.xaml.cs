using PizzeriaApp.Controllers;
using PizzeriaApp.Models;

namespace PizzeriaApp.Views
{
    // Esta vista se encarga de dar de alta nuevas pizzas o productos en el menú
    public partial class AltaProducto : ContentPage
    {
        // Guardamos la imagen en Base64 para mandarla directo a la base de datos sin líos de hostings externos
        private string _imageBase64 = "";
        private DataBaseServices _dbService;

        public AltaProducto()
        {
            InitializeComponent();
            // Instanciamos el servicio de base de datos para tenerlo listo al guardar
            _dbService = new DataBaseServices();
        }

        // Este evento se dispara cuando el admin quiere ponerle una foto a la nueva pizza
        private async void OnPickImageClicked(object sender, EventArgs e)
        {
            try
            {
                // Abrimos el selector de fotos nativo del celular
                var photo = await MediaPicker.Default.PickPhotoAsync();
                if (photo == null) return; // Si canceló, no hacemos nada

                // Leemos los bytes de la imagen; usamos un MemoryStream para no bloquear el archivo
                byte[] bytes;
                using (var stream = await photo.OpenReadAsync())
                using (var ms = new MemoryStream())
                {
                    await stream.CopyToAsync(ms);
                    bytes = ms.ToArray();
                }

                // Convertimos esos bytes a un string Base64, que es como Supabase lo espera recibir
                _imageBase64 = Convert.ToBase64String(bytes);

                // Actualizamos la vista previa en la pantalla para que el usuario vea qué eligió
                imgSelected.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
            }
            catch (Exception ex)
            {
                // Si algo falla (permisos, memoria, etc.), avisamos pero no tronamos la app
                await DisplayAlert("Error", "Fallo al seleccionar imagen: " + ex.Message, "OK");
            }
        }

        // El botón principal para meter la pizza al sistema
        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            // Validamos que por lo menos nos den el nombre y el precio, lo básico del POS
            if (string.IsNullOrEmpty(txtNombre.Text) || string.IsNullOrEmpty(txtPrecio.Text))
            {
                await DisplayAlert("Atención", "Nombre y Precio son requeridos.", "OK");
                return;
            }

            // Intentamos convertir el texto del precio a un número decimal válido
            if (!decimal.TryParse(txtPrecio.Text, out decimal precio))
            {
                await DisplayAlert("Error", "Precio inválido. Solo números por favor.", "OK");
                return;
            }

            // Bloqueamos el botón y cambiamos el texto para que el usuario no le pique mil veces mientras carga
            var btn = sender as Button;
            btn.IsEnabled = false;
            btn.Text = "Guardando...";

            try 
            {
                // Creamos el objeto Producto con los datos del formulario
                var nuevoProducto = new Producto
                {
                    Nombre = txtNombre.Text.Trim(),
                    Descripcion = txtDescripcion.Text?.Trim() ?? "", // Si no hay descripción, mandamos vacío
                    Precio = precio,
                    Categoria = pickCategoria.SelectedItem?.ToString() ?? "General",
                    ImagenBase64 = _imageBase64, // Aquí va el chorizo de texto de la imagen que capturamos arriba
                    Activo = true // Por defecto lo activamos para que salga en el menú
                };

                // Mandamos el objeto al servicio de Supabase
                bool ok = await _dbService.InsertarProductoAsync(nuevoProducto);

                if (ok)
                {
                    // Si todo salió bien, avisamos y regresamos a la pantalla de gestión
                    await DisplayAlert("¡Éxito!", "Producto añadido al catálogo.", "OK");
                    await Navigation.PopAsync();
                }
                else
                {
                    // Si el servicio nos dice que no, rehabilitamos el botón para reintentar
                    await DisplayAlert("Error", "No se pudo guardar en Supabase. Revisa tu conexión.", "OK");
                    btn.IsEnabled = true;
                    btn.Text = "GUARDAR PRODUCTO";
                }
            }
            catch (Exception ex)
            {
                // Cacho de seguridad por si truena algo inesperado en el mapeo o la red
                await DisplayAlert("Error Crítico", ex.Message, "OK");
                btn.IsEnabled = true;
                btn.Text = "GUARDAR PRODUCTO";
            }
        }
    }
}
