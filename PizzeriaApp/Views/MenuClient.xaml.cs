using PizzeriaApp.Models;
using PizzeriaApp.Controllers;

namespace PizzeriaApp.Views
{
    public partial class MenuClient : ContentPage
    {
        private readonly UsuarioPerfil _clienteActual;
        private readonly DataBaseServices _dbService;

        public MenuClient(UsuarioPerfil cliente)
        {
            InitializeComponent();
            _clienteActual = cliente;
            _dbService = new DataBaseServices();
        }

        private async void OnOrdenarClicked(object sender, EventArgs e)
        {
            btnOrdenar.IsEnabled = false;
            btnOrdenar.Text = "Procesando...";

            bool exito = await _dbService.CrearPedidoAsync(_clienteActual.Id, 250.00m);

            if (exito)
            {
                await DisplayAlert("¡Éxito!", "Tu pedido ha sido enviado a la cocina.", "Excelente");
            }
            else
            {
                await DisplayAlert("Error", "No pudimos procesar tu pedido. Intenta de nuevo.", "Ok");
            }

            btnOrdenar.IsEnabled = true;
            btnOrdenar.Text = "Confirmar y Ordenar";
        }
    }
}