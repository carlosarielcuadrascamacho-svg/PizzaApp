using System;
using System.Collections.ObjectModel;
using System.Linq;
using PizzeriaApp.Models;
using PizzeriaApp.Controllers;

namespace PizzeriaApp.Views
{
    // Clase auxiliar para mapear los datos que vamos a pintar en la tablita de tendencias
    public class MetricaConcepto
    {
        public string Concepto { get; set; }
        public int Cantidad { get; set; }
    }

    // Esta es la vista de "Business Intelligence" de la pizzería; aquí el dueño ve cuánto dinero ha entrado
    public partial class ReportesAdmin : ContentPage
    {
        private DataBaseServices _dbService;
        // Colección observable para que el DataGrid se actualice solito al llenar los datos
        public ObservableCollection<MetricaConcepto> ListaTendencias { get; set; }

        public ReportesAdmin()
        {
            InitializeComponent();
            _dbService = new DataBaseServices();
            ListaTendencias = new ObservableCollection<MetricaConcepto>();
        }

        // Cada vez que entran a ver las gráficas, recalculamos todo con la data más fresca
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LlenarReportesAsync();
        }

        // Método que hace el trabajo pesado de conteo y sumas
        private async Task LlenarReportesAsync()
        {
            // Consultamos las métricas reales directo de las vistas/funciones de Supabase
            var metricasReales = await _dbService.ObtenerMetricasRealesAsync();
            
            // Pintamos el dinero total con formato de moneda local
            lblIngresos.Text = metricasReales.IngresosTotales.ToString("C");
            
            // Limpiamos la lista de tendencias antes de volver a llenar
            ListaTendencias.Clear();
            int totalPlatosVendidos = 0;

            // Agarramos los 5 platos más vendidos para la tablita de popularidad
            foreach(var kvp in metricasReales.PlatillosPopulares.OrderByDescending(x => x.Value).Take(5))
            {
                ListaTendencias.Add(new MetricaConcepto { Concepto = kvp.Key, Cantidad = kvp.Value });
                totalPlatosVendidos += kvp.Value;
            }

            // Mostramos el conteo total de ítems despachados
            lblPlatillos.Text = $"{totalPlatosVendidos} Platillos";
            // Enlazamos la fuente de datos al control Grid de la pantalla
            dgTendencias.ItemsSource = ListaTendencias;
        }
    }
}
