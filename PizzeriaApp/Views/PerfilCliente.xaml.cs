using PizzeriaApp.Controllers;
using PizzeriaApp.Models;
using PizzeriaApp.Services;

namespace PizzeriaApp.Views
{
    // Esta es la Vista del Perfil; delega la lógica de usuario al AuthController
    public partial class PerfilCliente : ContentPage
    {
        private UsuarioPerfil _usuario;
        private AuthController _controller;
        private string _imageBase64 = "";

        public PerfilCliente(UsuarioPerfil usuario)
        {
            InitializeComponent();
            _usuario = usuario;
            // Inicialización del controlador con el servicio de perfiles
            _controller = new AuthController(null, new ServicioPerfiles());
            
            this.BindingContext = _usuario;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await RefrescarPerfilAsync();
        }

        private async Task RefrescarPerfilAsync()
        {
            try
            {
                // El controlador orquesta la obtención de datos frescos
                var perfilFresco = await _controller.ObtenerPerfilAsync(_usuario.Id);
                if (perfilFresco != null)
                {
                    _usuario = perfilFresco;
                    this.BindingContext = _usuario;

                    txtNombre.Text = _usuario.Nombre;
                    txtTelefono.Text = _usuario.Telefono;
                    txtDireccion.Text = _usuario.Direccion;
                    txtEmail.Text = _usuario.Email;
                    
                    if (!string.IsNullOrEmpty(_usuario.FotoPerfil))
                    {
                        _imageBase64 = _usuario.FotoPerfil;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error en controlador de perfil: " + ex.Message);
            }
        }

        private async void OnPickPhotoClicked(object sender, EventArgs e)
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
                imgPerfil.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "No se pudo cargar la imagen: " + ex.Message, "OK");
            }
        }

        private async void OnUpdateProfileClicked(object sender, EventArgs e)
        {
            var btn = sender as Button;
            btn.IsEnabled = false;
            btn.Text = "Guardando...";

            try 
            {
                _usuario.Nombre = txtNombre.Text?.Trim();
                _usuario.Telefono = txtTelefono.Text?.Trim();
                _usuario.Direccion = txtDireccion.Text?.Trim();
                _usuario.FotoPerfil = _imageBase64;

                // Delegamos la actualización al controlador
                bool ok = await _controller.ActualizarPerfilUsuarioAsync(_usuario.Id, _usuario.Nombre, _usuario.Direccion, _usuario.Telefono, _usuario.FotoPerfil);

                if (ok)
                {
                    await DisplayAlert("¡Excelente!", "Perfil actualizado correctamente.", "OK");
                }
                else
                {
                    await DisplayAlert("Error", "No pudimos sincronizar con el servidor.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ups", "Error inesperado: " + ex.Message, "OK");
            }
            finally
            {
                btn.IsEnabled = true;
                btn.Text = "ACTUALIZAR PERFIL";
            }
        }
    }
}
