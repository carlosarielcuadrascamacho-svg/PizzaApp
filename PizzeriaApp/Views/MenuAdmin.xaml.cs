using PizzeriaApp.Models;
using PizzeriaApp.Controllers; // Asegúrate de tener este using

namespace PizzeriaApp.Views
{
    public partial class MenuAdmin : ContentPage
    {
        private readonly DataBaseServices _dbService;

        public MenuAdmin()
        {
            InitializeComponent();
            _dbService = new DataBaseServices();
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarPedidosRealesAsync();
        }

        private async Task CargarPedidosRealesAsync()
        {
            var pedidosDeLaNube = await _dbService.ObtenerPedidosActivosAsync();
            GridPedidos.ItemsSource = pedidosDeLaNube;
        }
    }
}