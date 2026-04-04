using System;
using System.Collections.ObjectModel;
using System.Linq;
using PizzeriaApp.Models;
using PizzeriaApp.Controllers;

namespace PizzeriaApp.Views
{
    public class MetricaConcepto
    {
        public string Concepto { get; set; }
        public int Cantidad { get; set; }
    }

    public partial class ReportesAdmin : ContentPage
    {
        private DataBaseServices _dbService;
        public ObservableCollection<MetricaConcepto> ListaTendencias { get; set; }

        public ReportesAdmin()
        {
            InitializeComponent();
            _dbService = new DataBaseServices();
            ListaTendencias = new ObservableCollection<MetricaConcepto>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LlenarReportesAsync();
        }

        private async Task LlenarReportesAsync()
        {
            var metricasReales = await _dbService.ObtenerMetricasRealesAsync();
            lblIngresos.Text = metricasReales.IngresosTotales.ToString("C");
            
            ListaTendencias.Clear();
            int totalPlatosVendidos = 0;

            foreach(var kvp in metricasReales.PlatillosPopulares.OrderByDescending(x => x.Value).Take(5))
            {
                ListaTendencias.Add(new MetricaConcepto { Concepto = kvp.Key, Cantidad = kvp.Value });
                totalPlatosVendidos += kvp.Value;
            }

            lblPlatillos.Text = $"{totalPlatosVendidos} Platillos";
            PieRanking.ItemsSource = ListaTendencias;
        }
    }
}
