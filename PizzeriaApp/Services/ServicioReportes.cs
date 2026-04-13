using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using PizzeriaApp.Models;

namespace PizzeriaApp.Services
{
    /// <summary>
    /// Gestiona las métricas financieras y reportes de la administración.
    /// </summary>
    public class ServicioReportes : ServicioBase
    {
        public async Task<List<Pedido>> ObtenerPedidosCompletadosAsync()
        {
            try
            {
                var respuesta = await Client.From<Pedido>()
                    .Where(p => p.Estado == "Entregado")
                    .Order(p => p.Fecha, Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();

                return respuesta.Models;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServicioReportes] Error al obtener pedidos completados: {ex.Message}");
                return new List<Pedido>();
            }
        }

        public async Task<(decimal Ingresos, int Unidades, List<Pedido> Historial)> ObtenerMetricasDelDiaAsync()
        {
            try
            {
                var respuesta = await Client.From<Pedido>()
                    .Order(p => p.Fecha, Supabase.Postgrest.Constants.Ordering.Descending)
                    .Limit(200)
                    .Get();

                var historialHoy = respuesta.Models
                    .Where(p => p.FechaLocal.Date == DateTime.Today)
                    .ToList();

                var pedidosValidos = historialHoy.Where(p => p.Estado != "Cancelado").ToList();
                decimal ingresos = pedidosValidos.Sum(p => p.Total);

                // Enriquecemos con detalles para conteo de unidades
                await EnriquecerConDetalles(historialHoy);
                
                int unidades = historialHoy
                    .Where(p => p.Estado != "Cancelado")
                    .SelectMany(p => p.Detalles)
                    .Sum(d => d.Cantidad);

                return (ingresos, unidades, historialHoy);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServicioReportes] Error al calcular métricas: {ex.Message}");
                return (0, 0, new List<Pedido>());
            }
        }

        private async Task EnriquecerConDetalles(List<Pedido> pedidos)
        {
            if (!pedidos.Any()) return;

            var idsPedidos = pedidos.Select(p => p.Id).ToList();
            var detallesRes = await Client.From<DetallePedido>()
                .Filter("pedido_id", Supabase.Postgrest.Constants.Operator.In, idsPedidos)
                .Get();

            var productosRes = await Client.From<Producto>().Get();
            var mapaProductos = productosRes.Models.ToDictionary(p => p.Id, p => p.Nombre);

            foreach (var pedido in pedidos)
            {
                var susDetalles = detallesRes.Models.Where(d => d.PedidoId == pedido.Id).ToList();
                foreach (var d in susDetalles)
                {
                    d.NombrePlatillo = mapaProductos.ContainsKey(d.ProductoId) ? mapaProductos[d.ProductoId] : "??";
                }
                pedido.Detalles = new ObservableCollection<DetallePedido>(susDetalles);
            }
        }
    }
}
