using PizzeriaApp.Models;
using System.Collections.ObjectModel;

namespace PizzeriaApp.Views
{
    public partial class MenuAdmin : ContentPage
    {
        public MenuAdmin()
        {
            InitializeComponent();
            CargarPedidosPrueba();
        }

        private void CargarPedidosPrueba()
        {
            // ObservableCollection notifica a la UI si agregamos o quitamos elementos
            var lista = new ObservableCollection<Pedido>
            {
                new Pedido { Fecha = DateTime.Now, Estado = "En preparación", Total = 150.00m },
                new Pedido { Fecha = DateTime.Now.AddMinutes(-45), Estado = "En camino", Total = 320.50m },
                new Pedido { Fecha = DateTime.Now.AddHours(-2), Estado = "Entregado", Total = 210.00m }
            };

            // Enlazamos la lista a la tabla
            GridPedidos.ItemsSource = lista;
        }
    }
}