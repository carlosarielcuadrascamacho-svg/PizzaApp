using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using PizzeriaApp.Models;
using Supabase.Realtime;
using Supabase.Realtime.PostgresChanges;

namespace PizzeriaApp.Services
{
    /// <summary>
    /// Encargado de gestionar las transacciones de pedidos, cocina y tiempo real.
    /// </summary>
    public class ServicioPedidos : ServicioBase
    {
        private RealtimeChannel? _canalPedidos;

        // Caché local para optimizar las consultas repetitivas de nombres de productos y perfiles
        private static Dictionary<string, Producto> _cacheProductos = new();
        private static Dictionary<string, UsuarioPerfil> _cachePerfiles = new();
        private static DateTime _ultimaCargaCatalogo = DateTime.MinValue;

        /// <summary>
        /// Crea un nuevo pedido con sus detalles de forma transaccional.
        /// </summary>
        public async Task<bool> CrearPedidoAsync(string clienteId, List<ItemCarrito> carrito, decimal total, string mesa, string comentario = "")
        {
            try
            {
                var nuevoPedido = new Pedido
                {
                    ClienteId = clienteId,
                    Total = total,
                    Estado = "Ordenado",
                    Mesa = mesa,
                    Comentario = comentario,
                    Fecha = DateTime.UtcNow
                };

                // 1. Insertar el Pedido (Header)
                var respuestaPedido = await Client.From<Pedido>().Insert(nuevoPedido);
                var pedidoInsertado = respuestaPedido.Models.FirstOrDefault();

                if (pedidoInsertado == null) return false;

                // 2. Insertar los detalles
                var detalles = carrito.Select(item => new DetallePedido
                {
                    PedidoId = pedidoInsertado.Id,
                    ProductoId = item.Producto.Id,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.Producto.Precio
                }).ToList();

                await Client.From<DetallePedido>().Insert(detalles);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServicioPedidos] Error al crear pedido: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Obtiene los pedidos que la cocina todavía tiene que preparar, enriquecidos con datos del cliente y productos.
        /// </summary>
        public async Task<List<Pedido>> ObtenerPedidosActivosAsync()
        {
            try
            {
                var respuesta = await Client.From<Pedido>()
                    .Where(p => p.Estado != "Entregado" && p.Estado != "Cancelado")
                    .Order(p => p.Fecha, Supabase.Postgrest.Constants.Ordering.Ascending)
                    .Get();

                var pedidosActivos = respuesta.Models;
                if (!pedidosActivos.Any()) return pedidosActivos;

                await EnriquecerPedidosConDetalles(pedidosActivos);
                return pedidosActivos;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServicioPedidos] Error al obtener pedidos activos: {ex.Message}");
                return new List<Pedido>();
            }
        }

        private async Task EnriquecerPedidosConDetalles(List<Pedido> pedidos)
        {
            var idsPedidos = pedidos.Select(p => p.Id).ToList();
            
            // 1. Asegurar catálogo en caché
            if (!_cacheProductos.Any() || (DateTime.UtcNow - _ultimaCargaCatalogo).TotalMinutes > 10)
            {
                var productosRes = await Client.From<Producto>().Get();
                _cacheProductos = productosRes.Models.ToDictionary(p => p.Id, p => p);
                _ultimaCargaCatalogo = DateTime.UtcNow;
            }

            // 2. Cargar Detalles de todos los pedidos de golpe
            var detallesResponse = await Client.From<DetallePedido>()
                .Filter("pedido_id", Supabase.Postgrest.Constants.Operator.In, idsPedidos)
                .Get();

            // 3. Cargar perfiles faltantes
            var idsClientes = pedidos.Select(p => p.ClienteId).Distinct().Where(id => !_cachePerfiles.ContainsKey(id)).ToList();
            if (idsClientes.Any())
            {
                var perfilesRes = await Client.From<UsuarioPerfil>()
                    .Filter("id", Supabase.Postgrest.Constants.Operator.In, idsClientes)
                    .Get();
                foreach (var perfil in perfilesRes.Models) _cachePerfiles[perfil.Id] = perfil;
            }

            // 4. Mapear todo
            foreach (var pedido in pedidos)
            {
                var detallesDelPedido = detallesResponse.Models.Where(d => d.PedidoId == pedido.Id).ToList();
                foreach (var d in detallesDelPedido)
                {
                    d.NombrePlatillo = _cacheProductos.ContainsKey(d.ProductoId) ? _cacheProductos[d.ProductoId].Nombre : "??";
                }
                pedido.Detalles = new ObservableCollection<DetallePedido>(detallesDelPedido);
                
                if (_cachePerfiles.ContainsKey(pedido.ClienteId))
                    pedido.Cliente = _cachePerfiles[pedido.ClienteId];
            }
        }

        public async Task<bool> ActualizarEstadoPedidoAsync(string pedidoId, string nuevoEstado)
        {
            try
            {
                var res = await Client.From<Pedido>()
                    .Where(p => p.Id == pedidoId)
                    .Set(p => p.Estado, nuevoEstado)
                    .Update();
                return res.Models.Count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServicioPedidos] Error al actualizar estado: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Pedido>> ObtenerHistorialClienteAsync(string clienteId)
        {
            try
            {
                var respuesta = await Client.From<Pedido>()
                    .Where(p => p.ClienteId == clienteId)
                    .Order(p => p.Fecha, Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();

                var historial = respuesta.Models;
                if (historial.Any()) await EnriquecerPedidosConDetalles(historial);
                
                return historial;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServicioPedidos] Error al obtener historial cliente: {ex.Message}");
                return new List<Pedido>();
            }
        }

        public async Task SuscribirseAPedidosEnVivo(Action<Pedido> alCambiar)
        {
            DesuscribirsePedidosEnVivo();
            await InicializarAsync();

            _canalPedidos = await Client.From<Pedido>().On(PostgresChangesOptions.ListenType.All, (s, args) =>
            {
                var pedido = args.Model<Pedido>();
                alCambiar?.Invoke(pedido);
            });
        }

        public void DesuscribirsePedidosEnVivo()
        {
            if (_canalPedidos != null)
            {
                _canalPedidos.Unsubscribe();
                _canalPedidos = null;
            }
        }
    }
}
