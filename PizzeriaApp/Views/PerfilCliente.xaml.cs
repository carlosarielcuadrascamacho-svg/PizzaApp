using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Media;
using PizzeriaApp.Models;
using PizzeriaApp.Controllers;

namespace PizzeriaApp.Views
{
    public partial class PerfilCliente : ContentPage
    {
        private UsuarioPerfil _usuario;
        private DataBaseServices _dbService;
        private string _nuevaFotoBase64;

        public PerfilCliente(UsuarioPerfil usuario)
        {
            InitializeComponent();
            _usuario = usuario;
            _dbService = new DataBaseServices();
            BindingContext = _usuario;

            CargarDatosIniciales();
        }

        private void CargarDatosIniciales()
        {
            txtEmail.Text = _usuario.Email;
            txtNombre.Text = _usuario.Nombre ?? "";
            txtDireccion.Text = _usuario.Direccion ?? "";
            txtTelefono.Text = _usuario.Telefono ?? "";
            _nuevaFotoBase64 = _usuario.FotoPerfil;

            // Ocultamos el placeholder local de la camara si ya hay foto subida con Binding
            if (!string.IsNullOrEmpty(_usuario.FotoPerfil))
            {
                lblPlaceholderFoto.IsVisible = false;
            }
        }

        private async void OnCambiarFotoTapped(object sender, EventArgs e)
        {
            try
            {
                var photo = await MediaPicker.Default.PickPhotoAsync();

                if (photo != null)
                {
                    using (var stream = await photo.OpenReadAsync())
                    using (var memoryStream = new MemoryStream())
                    {
                        await stream.CopyToAsync(memoryStream);
                        byte[] imageBytes = memoryStream.ToArray();

                        // Codificación C# en cliente estricta segun el Request (No usar URLs en la nube de Storage)
                        _nuevaFotoBase64 = Convert.ToBase64String(imageBytes);

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            imgAvatar.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
                            lblPlaceholderFoto.IsVisible = false;
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Problema al elegir foto: {ex.Message}", "OK");
            }
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            btnGuardar.IsEnabled = false;
            btnGuardar.Text = "Guardando...";

            string nombre = txtNombre.Text?.Trim();
            string dir = txtDireccion.Text?.Trim();
            string tel = txtTelefono.Text?.Trim();

            bool exito = await _dbService.ActualizarPerfilAsync(_usuario.Id, nombre, dir, tel, _nuevaFotoBase64);

            if (exito)
            {
                // Actualizamos los datos in-memory en la sesion actual del Singleton
                _usuario.Nombre = nombre;
                _usuario.Direccion = dir;
                _usuario.Telefono = tel;
                _usuario.FotoPerfil = _nuevaFotoBase64;

                await DisplayAlert("Genial", "Tus datos han sido actualizados en la base de datos.", "OK");
                await Navigation.PopAsync();
            }
            else
            {
                await DisplayAlert("Error", "No se pudieron actualizar los datos. Revisa tu conexión.", "OK");
            }

            btnGuardar.IsEnabled = true;
            btnGuardar.Text = "Guardar Cambios";
        }
    }
}
