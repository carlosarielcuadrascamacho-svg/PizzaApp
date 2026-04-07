using PizzeriaApp.Controllers;
using PizzeriaApp.Models;

namespace PizzeriaApp.Views
{
    public partial class PerfilCliente : ContentPage
    {
        private UsuarioPerfil _usuario;
        private DataBaseServices _dbService;
        private string _imageBase64 = "";

        public PerfilCliente(UsuarioPerfil usuario)
        {
            InitializeComponent();
            _usuario = usuario;
            _dbService = new DataBaseServices();
            
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
                // Sincronizar con la BD (Regla de Oro del Perfil)
                var perfilFresco = await _dbService.ObtenerPerfilAsync(_usuario.Id);
                if (perfilFresco != null)
                {
                    _usuario = perfilFresco;
                    this.BindingContext = _usuario;

                    // Hidratar campos
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
                Console.WriteLine("Error refreshing profile: " + ex.Message);
            }
        }

        private async void OnPickPhotoClicked(object sender, EventArgs e)
        {
            try
            {
                var photo = await MediaPicker.Default.PickPhotoAsync();
                if (photo == null) return;

                // VISUALIZACIÓN INSTANTÁNEA (Feedback inmediato para el usuario)
                var stream = await photo.OpenReadAsync();
                imgPerfil.Source = ImageSource.FromStream(() => stream);

                // Conversión asíncrona a Base64 para el Guardado
                using (var ms = new MemoryStream())
                {
                    var fullStream = await photo.OpenReadAsync();
                    await fullStream.CopyToAsync(ms);
                    _imageBase64 = Convert.ToBase64String(ms.ToArray());
                }
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
                _usuario.FotoPerfil = _imageBase64; // Guardar el string nuevo o el anterior

                bool ok = await _dbService.ActualizarPerfilAsync(_usuario.Id, _usuario.Nombre, _usuario.Direccion, _usuario.Telefono, _usuario.FotoPerfil);

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
                await DisplayAlert("Ups", ex.Message, "OK");
            }
            finally
            {
                btn.IsEnabled = true;
                btn.Text = "ACTUALIZAR PERFIL";
            }
        }
    }
}
