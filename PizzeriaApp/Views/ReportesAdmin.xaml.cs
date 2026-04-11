using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using PizzeriaApp.Models;
using PizzeriaApp.Controllers;
using Syncfusion.Maui.DataGrid;

namespace PizzeriaApp.Views
{
    // Esta es la vista directiva de la pizzería; aquí se visualiza el progreso y los pedidos del día
    public partial class ReportesAdmin : ContentPage
    {
        private DataBaseServices _dbService;
        // Colección observable para que el DataGrid refleje inmediatamente los pedidos del día
        public ObservableCollection<Pedido> HistorialDia { get; set; }

        public ReportesAdmin()
        {
            InitializeComponent();
            _dbService = new DataBaseServices();
            HistorialDia = new ObservableCollection<Pedido>();
        }

        // Cada vez que entran a ver las gráficas, recalculamos todo con la data más fresca del día
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LlenarReportesAsync();
        }

        // Método que solicita las métricas diarias y el historial de pedidos
        private async Task LlenarReportesAsync()
        {
            // Consultamos las métricas restringidas al día de hoy
            var metricasDelDia = await _dbService.ObtenerMetricasDelDiaAsync();
            
            // Pintamos el dinero total de hoy con formato de moneda local
            lblIngresos.Text = metricasDelDia.IngresosTotales.ToString("C");
            
            // Mostramos el conteo total de unidades vendidas hoy
            lblPlatillos.Text = $"{metricasDelDia.UnidadesVendidas} Und.";

            // Limpiamos la lista y agregamos el nuevo historial de hoy
            HistorialDia.Clear();

            foreach(var pedido in metricasDelDia.HistorialHoy)
            {
                HistorialDia.Add(pedido);
            }

            // Enlazamos la fuente de datos al control Grid de la pantalla para mostrar el historial
            dgTendencias.ItemsSource = HistorialDia;
        }

        // Abrimos el detalle del ticket cuando el admin toca una fila
        private async void dgTendencias_SelectionChanged(object sender, DataGridSelectionChangedEventArgs e)
        {
            if (e.AddedRows != null && e.AddedRows.Count > 0)
            {
                // Obtenemos el objeto Pedido de la fila seleccionada
                var pedidoSeleccionado = e.AddedRows[0] as Pedido;

                if (pedidoSeleccionado != null)
                {
                    // Navegamos a la pantalla de detalle pasándole el pedido y el servicio de DB
                    await Navigation.PushAsync(new DetalleOrden(pedidoSeleccionado, _dbService));
                }

                // Deseleccionamos para que la fila no se quede marcada y permita volver a entrar si hace falta
                dgTendencias.SelectedIndex = -1;
            }
        }
    }
}
