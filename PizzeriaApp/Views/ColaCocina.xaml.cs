using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using PizzeriaApp.Models;
using PizzeriaApp.Controllers;
using Microsoft.Maui.Media; // En caso de requirir vibracion/beep

namespace PizzeriaApp.Views
{
    public partial class ColaCocina : ContentPage
    {
        private ObservableCollection<Pedido> _pedidosActivos;
        private DataBaseServices _dbService;

        public ColaCocina()
        {
            InitializeComponent();
            _dbService = new DataBaseServices();
            _pedidosActivos = new ObservableCollection<Pedido>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarPedidosAsync();
            await _dbService.SuscribirseAPedidosEnVivo((nuevoPedido) => 
            {
                MainThread.BeginInvokeOnMainThread(() => 
                {
                    _pedidosActivos.Insert(0, nuevoPedido);
                });
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _dbService.DesuscribirsePedidosEnVivo();
        }

        private async Task CargarPedidosAsync()
        {
            var pedidosDb = await _dbService.ObtenerPedidosActivosAsync();
            _pedidosActivos.Clear();
            foreach (var p in pedidosDb)
            {
                _pedidosActivos.Add(p);
            }
            ListaPedidos.ItemsSource = _pedidosActivos;
        }

        private async void OnDespacharClicked(object sender, EventArgs e)
        {
            var boton = sender as Button;
            var pedido = boton?.BindingContext as Pedido;

            if(pedido != null)
            {
                bool confirmado = await DisplayAlert("Confirmación", $"¿Marcar orden {pedido.Id.Substring(0,8)} como Entregado?", "Sí", "Cancelar");
                if (confirmado)
                {
                    bool exito = await _dbService.ActualizarEstadoPedidoAsync(pedido.Id, "Entregado");
                    if (exito)
                    {
                        _pedidosActivos.Remove(pedido);
                    }
                }
            }
        }
    }
}
