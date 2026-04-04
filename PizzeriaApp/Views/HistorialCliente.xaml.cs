using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using PizzeriaApp.Models;
using PizzeriaApp.Controllers;

namespace PizzeriaApp.Views
{
    public partial class HistorialCliente : ContentPage
    {
        private string _clienteId;
        private DataBaseServices _dbService;

        public HistorialCliente(string clienteId)
        {
            InitializeComponent();
            _clienteId = clienteId;
            _dbService = new DataBaseServices();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarHistorialAsync();
        }

        private async Task CargarHistorialAsync()
        {
            var historial = await _dbService.ObtenerHistorialClienteAsync(_clienteId);
            ListaHistorial.ItemsSource = historial;
        }
    }
}
