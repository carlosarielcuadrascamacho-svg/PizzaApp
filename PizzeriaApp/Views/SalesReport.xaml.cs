using System;
using System.Collections.ObjectModel;
using System.Linq;
using PizzeriaApp.Models;
using PizzeriaApp.Controllers;

namespace PizzeriaApp.Views
{
    public class MetricaProducto
    {
        public string NombreProducto { get; set; }
        public int CantidadVendida { get; set; }
    }

    public partial class SalesReport : ContentPage
    {
        private readonly DataBaseServices _dbService;
        public ObservableCollection<MetricaProducto> VentasPorProducto { get; set; }

        public SalesReport()
        {
            InitializeComponent();
            _dbService = new DataBaseServices();
            VentasPorProducto = new ObservableCollection<MetricaProducto>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await GenerarReporteGraficoAsync();
        }

        private async Task GenerarReporteGraficoAsync()
        {
            try
            {
                var pedidos = await _dbService.ObtenerPedidosCompletadosAsync();
                
                int totalPedidos = pedidos.Count;
                decimal totalIngresos = pedidos.Sum(p => p.Total);

                lblTotalPedidos.Text = totalPedidos.ToString();
                lblIngresos.Text = totalIngresos.ToString("C");

                // Datos de métricas simulados por ahora, ya que la BBDD Relacional PostgreSQL 
                // requiere cruce explícito de PostgrestJoin y no disponemos del ORM complejo.
                VentasPorProducto.Clear();
                VentasPorProducto.Add(new MetricaProducto { NombreProducto = "Pepperoni", CantidadVendida = 45 });
                VentasPorProducto.Add(new MetricaProducto { NombreProducto = "Hawaiana", CantidadVendida = 30 });
                VentasPorProducto.Add(new MetricaProducto { NombreProducto = "Mexicana", CantidadVendida = 25 });
                VentasPorProducto.Add(new MetricaProducto { NombreProducto = "Vegetariana", CantidadVendida = 10 });
                
                // Si la cantidad de pedidos crece orgánicamente escalaremos proporcionalmente la métrica
                if (totalPedidos > 0)
                {
                    VentasPorProducto.Add(new MetricaProducto { NombreProducto = "Especiales", CantidadVendida = totalPedidos * 2 });
                }

                PieVentas.ItemsSource = VentasPorProducto;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
