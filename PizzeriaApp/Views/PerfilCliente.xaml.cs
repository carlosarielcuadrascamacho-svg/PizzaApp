using PizzeriaApp.Controllers;
using PizzeriaApp.Models;

namespace PizzeriaApp.Views
{
    // Pantalla para que el cliente gestione sus datos personales y su foto de perfil
    public partial class PerfilCliente : ContentPage
    {
        private UsuarioPerfil _usuario;
        private DataBaseServices _dbService;
        // Aquí guardaremos la foto en Base64 para subirla a la tabla de perfiles
        private string _imageBase64 = "";

        public PerfilCliente(UsuarioPerfil usuario)
        {
            InitializeComponent();
            _usuario = usuario;
            _dbService = new DataBaseServices();
            
            // Usamos el objeto usuario como fuente de datos para los bindings del XAML
            this.BindingContext = _usuario;
        }

        // Siempre que entramos, refrescamos por si cambió algo en otro dispositivo o sesión
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await RefrescarPerfilAsync();
        }

        // Jalamos la info más fresca de Supabase para evitar data vieja en la UI
        private async Task RefrescarPerfilAsync()
        {
            try
            {
                // Vamos por el perfil a la BD usando el ID único del usuario
                var perfilFresco = await _dbService.ObtenerPerfilAsync(_usuario.Id);
                if (perfilFresco != null)
                {
                    _usuario = perfilFresco;
                    // Actualizamos el contexto de binding para que los labels/inputs se refresquen
                    this.BindingContext = _usuario;

                    // Llenamos los campos de texto con la info recibida
                    txtNombre.Text = _usuario.Nombre;
                    txtTelefono.Text = _usuario.Telefono;
                    txtDireccion.Text = _usuario.Direccion;
                    txtEmail.Text = _usuario.Email;
                    
                    // Si ya tiene foto, guardamos su Base64 por si solo quieren editar el nombre
                    if (!string.IsNullOrEmpty(_usuario.FotoPerfil))
                    {
                        _imageBase64 = _usuario.FotoPerfil;
                    }
                }
            }
            catch (Exception ex)
            {
                // Un log discreto por si la red anda fallando
                Console.WriteLine("Error refreshing profile: " + ex.Message);
            }
        }

        // Evento para cambiar la foto de perfil usando la cámara o galería
        private async void OnPickPhotoClicked(object sender, EventArgs e)
        {
            try
            {
                var photo = await MediaPicker.Default.PickPhotoAsync();
                if (photo == null) return;

                // Leemos los bytes de la imagen seleccionada
                byte[] bytes;
                using (var stream = await photo.OpenReadAsync())
                using (var ms = new MemoryStream())
                {
                    await stream.CopyToAsync(ms);
                    bytes = ms.ToArray();
                }

                // Convertimos a Base64; técnica estándar en esta app para no lidiar con storage complejo
                _imageBase64 = Convert.ToBase64String(bytes);

                // Mostramos la imagen de inmediato en el círculo de perfil
                imgPerfil.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo cargar la imagen: " + ex.Message, "OK");
            }
        }

        // El botón para confirmar los cambios en los datos del cliente
        private async void OnUpdateProfileClicked(object sender, EventArgs e)
        {
            // Feedback visual de "estoy trabajando"
            var btn = sender as Button;
            btn.IsEnabled = false;
            btn.Text = "Guardando...";

            try 
            {
                // Mapeamos lo que el usuario escribió de vuelta al objeto
                _usuario.Nombre = txtNombre.Text?.Trim();
                _usuario.Telefono = txtTelefono.Text?.Trim();
                _usuario.Direccion = txtDireccion.Text?.Trim();
                _usuario.FotoPerfil = _imageBase64; // Mandamos el chorizo de texto de la foto

                // Intentamos guardar en Supabase
                bool ok = await _dbService.ActualizarPerfilAsync(_usuario.Id, _usuario.Nombre, _usuario.Direccion, _usuario.Telefono, _usuario.FotoPerfil);

                if (ok)
                {
                    await DisplayAlert("¡Excelente!", "Perfil actualizado correctamente.", "OK");
                }
                else
                {
                    await DisplayAlert("Error", "No pudimos sincronizar con el servidor. Revisa tu internet.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ups", "Ocurrió un error inesperado: " + ex.Message, "OK");
            }
            finally
            {
                // Regresamos el botón a la vida
                btn.IsEnabled = true;
                btn.Text = "ACTUALIZAR PERFIL";
            }
        }
    }
}
