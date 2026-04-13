using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using PizzeriaApp.Models;
using PizzeriaApp.Controllers;
using PizzeriaApp.Services;
using Syncfusion.Maui.DataGrid;

namespace PizzeriaApp.Views
{
    // Esta es la Vista de reportes; delega la carga de datos al AdminController
    public partial class ReportesAdmin : ContentPage
    {
        private AdminController _controller;
        public ObservableCollection<Pedido> HistorialDia { get; set; }

        public ReportesAdmin()
        {
            InitializeComponent();
            // Inicialización del controlador con servicios especializados
            _controller = new AdminController(new ServicioReportes(), new ServicioPedidos(), new ServicioCatalogo());
            HistorialDia = new ObservableCollection<Pedido>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LlenarReportesAsync();
        }

        private async Task LlenarReportesAsync()
        {
            var result = await _controller.ObtenerReportesDiaAsync();
            
            // 1. ASIGNACIÓN DE KPIs BÁSICOS
            lblIngresos.Text = result.Ingresos.ToString("C");
            lblPlatillos.Text = $"{result.Unidades} Und.";
            lblOrdenesTotales.Text = result.Historial.Count.ToString();

            // 2. CÁLCULO DE TICKET PROMEDIO (Solo pedidos no cancelados)
            var pedidosValidos = result.Historial.Where(p => p.Estado != "Cancelado").ToList();
            decimal ticketPromedio = pedidosValidos.Any() ? result.Ingresos / pedidosValidos.Count : 0;
            lblTicketPromedio.Text = ticketPromedio.ToString("C");

            // 3. ACTUALIZAR LISTA
            HistorialDia.Clear();
            foreach(var pedido in result.Historial)
            {
                HistorialDia.Add(pedido);
            }
            dgTendencias.ItemsSource = HistorialDia;

            // 4. ACTUALIZAR GRÁFICAS
            ActualizarGraficas(result.Historial);
        }

        private void ActualizarGraficas(List<Pedido> pedidos)
        {
            // --- Gráfica de Distribución (Donut) ---
            var datosDistribucion = pedidos
                .Where(p => p.Estado != "Cancelado")
                .SelectMany(p => p.Detalles)
                .GroupBy(d => d.NombrePlatillo)
                .Select(g => new ChartData { Name = g.Key, Value = (double)g.Sum(x => x.Subtotal) })
                .OrderByDescending(x => x.Value)
                .Take(5) // Top 5 platillos
                .ToList();

            donutSeries.ItemsSource = datosDistribucion;

            // --- Gráfica de Tendencia Horaria (Area Chart) ---
            // Agrupamos ventas por la hora de la fecha local
            var datosTendencia = pedidos
                .Where(p => p.Estado != "Cancelado")
                .GroupBy(p => p.FechaLocal.Hour)
                .Select(g => new ChartData { 
                    Name = $"{g.Key:00}:00", 
                    Value = (double)g.Sum(p => p.Total),
                    Order = g.Key 
                })
                .OrderBy(x => x.Order)
                .ToList();

            // Si hay pocos puntos, rellenamos las horas faltantes del día para que la gráfica se vea completa
            trendSeries.ItemsSource = datosTendencia;
        }

        public class ChartData
        {
            public string Name { get; set; }
            public double Value { get; set; }
            public int Order { get; set; }
        }

        private async void dgTendencias_SelectionChanged(object sender, DataGridSelectionChangedEventArgs e)
        {
            if (e.AddedRows != null && e.AddedRows.Count > 0)
            {
                var pedidoSeleccionado = e.AddedRows[0] as Pedido;

                if (pedidoSeleccionado != null)
                {
                    // Navegamos pasando los servicios necesarios para el administrador
                    await Navigation.PushAsync(new DetalleOrden(pedidoSeleccionado));
                }

                dgTendencias.SelectedIndex = -1;
            }
        }
    }
}
