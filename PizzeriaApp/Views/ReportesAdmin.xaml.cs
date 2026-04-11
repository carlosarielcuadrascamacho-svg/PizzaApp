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
            // Inicialización del controlador
            _controller = new AdminController(new DataBaseServices());
            HistorialDia = new ObservableCollection<Pedido>();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LlenarReportesAsync();
        }

        private async Task LlenarReportesAsync()
        {
            // El controlador nos da las métricas ya calculadas
            var result = await _controller.ObtenerReportesDiaAsync();
            
            lblIngresos.Text = result.Ingresos.ToString("C");
            lblPlatillos.Text = $"{result.Unidades} Und.";

            HistorialDia.Clear();
            foreach(var pedido in result.Historial)
            {
                HistorialDia.Add(pedido);
            }

            dgTendencias.ItemsSource = HistorialDia;
        }

        private async void dgTendencias_SelectionChanged(object sender, DataGridSelectionChangedEventArgs e)
        {
            if (e.AddedRows != null && e.AddedRows.Count > 0)
            {
                var pedidoSeleccionado = e.AddedRows[0] as Pedido;

                if (pedidoSeleccionado != null)
                {
                    // Navegamos pasando el servicio (requerido por el constructor de DetalleOrden)
                    await Navigation.PushAsync(new DetalleOrden(pedidoSeleccionado, new DataBaseServices()));
                }

                dgTendencias.SelectedIndex = -1;
            }
        }
    }
}
