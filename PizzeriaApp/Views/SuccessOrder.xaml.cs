using PizzeriaApp.Models;

namespace PizzeriaApp.Views
{
    public partial class SuccessOrder : ContentPage
    {
        private readonly UsuarioPerfil _usuario;

        public SuccessOrder(UsuarioPerfil usuario)
        {
            InitializeComponent();
            _usuario = usuario;
        }

        private async void OnSeguirPedidoClicked(object sender, EventArgs e)
        {
            // Navegamos al historial para que vea el timeline (pasamos ID y Nombre)
            await Navigation.PushAsync(new HistorialCliente(_usuario.Id, _usuario.Nombre));
            
            // Removemos esta pantalla del stack para que no pueda volver atrás
            var pageToRemove = Navigation.NavigationStack.FirstOrDefault(p => p is SuccessOrder);
            if (pageToRemove != null) Navigation.RemovePage(pageToRemove);
        }

        private async void OnVolverInicioClicked(object sender, EventArgs e)
        {
            await Navigation.PopToRootAsync();
        }
    }
}
