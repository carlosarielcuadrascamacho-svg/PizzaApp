using PizzeriaApp.Models;
using PizzeriaApp.Controllers;

namespace PizzeriaApp.Views
{
    public partial class MenuClient : ContentPage
    {
        private readonly UsuarioPerfil _clienteActual;
        private readonly DataBaseServices _dbService;

        // Modificamos el constructor para exigir el perfil del cliente
        public MenuClient(UsuarioPerfil cliente)
        {
            InitializeComponent();
            _clienteActual = cliente;
            _dbService = new DataBaseServices();
        }

        private async void OnOrdenarClicked(object sender, EventArgs e)
        {
            // Deshabilitamos el botón para evitar dobles envíos
            btnOrdenar.IsEnabled = false;
            btnOrdenar.Text = "Procesando...";

            // Enviamos el ID real del usuario validado en Google y el costo de la pizza
            bool exito = await _dbService.CrearPedidoAsync(_clienteActual.Id, 250.00m);

            if (exito)
            {
                await DisplayAlert("¡Éxito!", "Tu pedido ha sido enviado a la cocina.", "Excelente");
            }
            else
            {
                await DisplayAlert("Error", "No pudimos procesar tu pedido. Intenta de nuevo.", "Ok");
            }

            // Restauramos el botón
            btnOrdenar.IsEnabled = true;
            btnOrdenar.Text = "Confirmar y Ordenar";
        }
    }
}