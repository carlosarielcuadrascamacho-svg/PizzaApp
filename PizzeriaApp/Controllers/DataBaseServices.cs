using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PizzeriaApp.Models;
using Supabase;
using PizzeriaApp.Config;
using Supabase.Realtime;
using Supabase.Realtime.PostgresChanges;
using PizzeriaApp.Services;

namespace PizzeriaApp.Controllers
{
    public class DataBaseServices
    {
        private Supabase.Client? _supabase;
        // Acceso seguro al cliente: obtiene el cliente inicializado o lanza si no está listo
        private Supabase.Client Client => _supabase ?? SupabaseClientFactory.GetClientOrThrow();
        private bool _initialized = false;

        public DataBaseServices()
        {
            // Usamos el cliente singleton proporcionado por SupabaseClientFactory
            _supabase = SupabaseClientFactory.Client;
        }

        // Delegamos la inicialización al factory único
        public async Task InitializeAsync()
        {
            if (_initialized)
                return;

            try
            {
                // Inicializa el cliente global una sola vez
                await SupabaseClientFactory.InitializeAsync(Secretos.SupabaseUrl, Secretos.SupabaseApiKey);
                // Actualizamos la referencia local al cliente singleton
                _supabase = SupabaseClientFactory.Client;
                _initialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inicializando Supabase desde DataBaseServices: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> InsertarPerfilAsync(string correo)
        {
            try
            {
                var nuevoPerfil = new UsuarioPerfil { Email = correo, EsAdmin = false };
                var respuesta = await Client.From<UsuarioPerfil>().Insert(nuevoPerfil);
                return respuesta.Models.Count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }

        public async Task<UsuarioPerfil?> ObtenerPerfilAsync(string idUsuario)
        {
            try
            {
                var resultado = await Client.From<UsuarioPerfil>().Where(p => p.Id == idUsuario).Get();
                return resultado.Models.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo perfil completo: {ex.Message}");
                return null;
            }
        }

        public async Task<string?> ObtenerIdPorEmailAsync(string correoBusqueda)
        {
            try
            {
                var resultado = await Client.From<UsuarioPerfil>().Where(p => p.Email == correoBusqueda).Get();
                return resultado.Models.FirstOrDefault()?.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> EsUsuarioAdminAsync(string IdBusqueda)
        {
            try
            {
                var resultado = await Client.From<UsuarioPerfil>().Where(p => p.Id == IdBusqueda).Get();
                return resultado.Models.FirstOrDefault()?.EsAdmin ?? false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Producto>> ObtenerProductosActivosAsync()
        {
            try
            {
                var respuesta = await Client.From<Producto>().Where(p => p.Activo == true).Get();
                return respuesta.Models;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener catálogo: {ex.Message}");
                return new List<Producto>();
            }
        }

        public async Task<bool> CrearPedidoCompletoAsync(string clienteId, List<ItemCarrito> carrito, decimal totalCalculado, string estadoInicial)
        {
            try
            {
                var nuevoPedido = new Pedido
                {
                    ClienteId = clienteId,
                    Total = totalCalculado,
                    Estado = estadoInicial,
                    Fecha = DateTime.UtcNow
                };

                var respuestaPedido = await Client.From<Pedido>().Insert(nuevoPedido);
                var pedidoInsertado = respuestaPedido.Models.FirstOrDefault();

                if (pedidoInsertado == null) return false;

                var detalles = new List<DetallePedido>();
                foreach (var item in carrito)
                {
                    detalles.Add(new DetallePedido
                    {
                        PedidoId = pedidoInsertado.Id,
                        ProductoId = item.Producto.Id,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = item.Producto.Precio
                    });
                }

                await Client.From<DetallePedido>().Insert(detalles);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en transacción: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CrearPedidoV2Async(string clienteId, List<ItemCarrito> carrito, decimal totalCalculado, string estadoInicial, string mesa, string comentario = "")
        {
            try
            {
                var nuevoPedido = new Pedido
                {
                    ClienteId = clienteId,
                    Total = totalCalculado,
                    Estado = estadoInicial,
                    Mesa = mesa,
                    Comentario = comentario,
                    Fecha = DateTime.UtcNow
                };

                var respuestaPedido = await Client.From<Pedido>().Insert(nuevoPedido);
                var pedidoInsertado = respuestaPedido.Models.FirstOrDefault();

                if (pedidoInsertado == null) return false;

                var detalles = new List<DetallePedido>();
                foreach (var item in carrito)
                {
                    detalles.Add(new DetallePedido
                    {
                        PedidoId = pedidoInsertado.Id,
                        ProductoId = item.Producto.Id,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = item.Producto.Precio
                    });
                }

                await Client.From<DetallePedido>().Insert(detalles);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en transacción v2: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> InsertarProductoAsync(Producto nuevoProducto)
        {
            try
            {
                // Guardar el producto en la tabla 'productos'
                var respuesta = await Client.From<Producto>().Insert(nuevoProducto);
                return respuesta.Models.Count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al insertar producto: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ActualizarProductoAsync(Producto p)
        {
            try
            {
                var actualizacion = await Client.From<Producto>()
                    .Where(x => x.Id == p.Id)
                    .Set(x => x.Nombre, p.Nombre)
                    .Set(x => x.Descripcion, p.Descripcion)
                    .Set(x => x.Precio, p.Precio)
                    .Set(x => x.Categoria, p.Categoria)
                    .Set(x => x.ImagenBase64, p.ImagenBase64)
                    .Set(x => x.Activo, p.Activo)
                    .Update();

                return actualizacion.Models.Count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al actualizar producto: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Pedido>> ObtenerPedidosActivosAsync()
        {
            try
            {
                // Excluir pedidos Entregados Y Cancelados
                var respuesta = await Client.From<Pedido>()
                    .Where(p => p.Estado != "Entregado" && p.Estado != "Cancelado")
                    .Order(p => p.Fecha, Supabase.Postgrest.Constants.Ordering.Ascending)
                    .Get();

                var pedidosActivos = respuesta.Models;
                var idsPedidos = pedidosActivos.Select(p => p.Id).ToList();

                if (idsPedidos.Any())
                {
                    // Traer detalles y productos en paralelo para mayor velocidad
                    var detallesTask = Client.From<DetallePedido>()
                        .Filter("pedido_id", Supabase.Postgrest.Constants.Operator.In, idsPedidos)
                        .Get();
                    var productosTask = Client.From<Producto>().Get();

                    await Task.WhenAll(detallesTask, productosTask);

                    var detallesResponse = detallesTask.Result;
                    var productosMapa = productosTask.Result.Models.ToDictionary(p => p.Id, p => p.Nombre);

                    foreach (var pedido in pedidosActivos)
                    {
                        var detallesDelPedido = detallesResponse.Models.Where(d => d.PedidoId == pedido.Id).ToList();
                        foreach (var d in detallesDelPedido)
                        {
                            d.NombrePlatillo = productosMapa.ContainsKey(d.ProductoId) ? productosMapa[d.ProductoId] : "Indefinido";
                        }
                        pedido.Detalles = new System.Collections.ObjectModel.ObservableCollection<DetallePedido>(detallesDelPedido);
                    }
                }

                return pedidosActivos;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener pedidos: {ex.Message}");
                return new List<Pedido>();
            }
        }

        public async Task<List<Pedido>> ObtenerPedidosCompletadosAsync()
        {
            try
            {
                // Traemos los que han sido entregados
                var respuesta = await Client.From<Pedido>()
                    .Where(p => p.Estado == "Entregado")
                    .Order(p => p.Fecha, Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();

                return respuesta.Models;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener pedidos completados: {ex.Message}");
                return new List<Pedido>();
            }
        }

        public async Task<bool> ActualizarEstadoPedidoAsync(string pedidoId, string nuevoEstado)
        {
            try
            {
                var actualizacion = await Client.From<Pedido>()
                    .Where(p => p.Id == pedidoId)
                    .Set(p => p.Estado, nuevoEstado)
                    .Update();

                return actualizacion.Models.Count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al actualizar pedido: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ActualizarPerfilAsync(string id, string nombre, string direccion, string telefono, string fotoBase64)
        {
            try
            {
                var actualizacion = await Client.From<UsuarioPerfil>()
                    .Where(p => p.Id == id)
                    .Set(p => p.Nombre, nombre)
                    .Set(p => p.Direccion, direccion)
                    .Set(p => p.Telefono, telefono)
                    .Set(p => p.FotoPerfil, fotoBase64)
                    .Update();
                return actualizacion.Models.Count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando perfil: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CambiarEstadoProductoAsync(string id, bool activo)
        {
            try
            {
                var actualizacion = await Client.From<Producto>()
                    .Where(p => p.Id == id)
                    .Set(p => p.Activo, activo)
                    .Update();
                return actualizacion.Models.Count > 0;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error deshabilitando producto: {ex.Message}");
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
                var idsPedidos = historial.Select(p => p.Id).ToList();

                if (idsPedidos.Any())
                {
                    var detallesResponse = await Client.From<DetallePedido>()
                        .Filter("pedido_id", Supabase.Postgrest.Constants.Operator.In, idsPedidos)
                        .Get();
                    
                    var productosResponse = await Client.From<Producto>().Get();
                    var productosMapa = productosResponse.Models.ToDictionary(p => p.Id, p => p.Nombre);

                    foreach (var pedido in historial)
                    {
                        var detallesDelPedido = detallesResponse.Models.Where(d => d.PedidoId == pedido.Id).ToList();
                        foreach (var d in detallesDelPedido)
                        {
                            d.NombrePlatillo = productosMapa.ContainsKey(d.ProductoId) ? productosMapa[d.ProductoId] : "??";
                        }
                        pedido.Detalles = new System.Collections.ObjectModel.ObservableCollection<DetallePedido>(detallesDelPedido);
                    }
                }

                return historial;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo historial: {ex.Message}");
                return new List<Pedido>();
            }
        }

        public async Task<List<Producto>> ObtenerCatalogoCompletoAsync()
        {
            try
            {
                // Ignora el filtro de activo=true
                var respuesta = await Client.From<Producto>().Get();
                return respuesta.Models;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo catálogo completo: {ex.Message}");
                return new List<Producto>();
            }
        }

        // Se usa una estructura para retornar tanto Total de Ingresos como las Ventas por Producto puras
        public async Task<(decimal IngresosTotales, Dictionary<string, int> PlatillosPopulares)> ObtenerMetricasRealesAsync()
        {
            decimal totalCaja = 0;
            var platillosRanking = new Dictionary<string, int>();

            try
            {
                // Solo calculamos sobre los pedidos completados/entregados
                var todosLosPedidos = await Client.From<Pedido>()
                    .Where(p => p.Estado == "Entregado")
                    .Get();

                var pedidos = todosLosPedidos.Models;
                totalCaja = pedidos.Sum(p => p.Total);

                // Sacamos todos los Ids de los pedidos entregados
                var idsPedidos = pedidos.Select(p => p.Id).ToList();

                if (idsPedidos.Any())
                {
                    // Filtrar desde la base de datos (PostgreSQL) usando operador IN (No descargar la tabla entera a RAM)
                    var detallesResponse = await Client.From<DetallePedido>()
                        .Filter("pedido_id", Supabase.Postgrest.Constants.Operator.In, idsPedidos)
                        .Get();
                    
                    var detallesValidos = detallesResponse.Models;

                    // Descargamos nombre de productos para cruzar (Mock PostgreSQL Join en código porque no hay ORM)
                    var productosResponse = await Client.From<Producto>().Get();
                    var productosMapa = productosResponse.Models.ToDictionary(p => p.Id, p => p.Nombre);

                    foreach (var det in detallesValidos)
                    {
                        var nombreReal = productosMapa.ContainsKey(det.ProductoId) ? productosMapa[det.ProductoId] : "Indefinido";
                        if (!platillosRanking.ContainsKey(nombreReal))
                            platillosRanking[nombreReal] = 0;
                        
                        platillosRanking[nombreReal] += det.Cantidad;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en métricas reales: {ex.Message}");
            }

            return (totalCaja, platillosRanking);
        }

        private RealtimeChannel? _canalPedidos;

        public void DesuscribirsePedidosEnVivo()
        {
            if (_canalPedidos != null)
            {
                _canalPedidos.Unsubscribe();
                _canalPedidos = null;
            }
        }

        public async Task SuscribirseAPedidosEnVivo(Action<Pedido> alRecibirCambio)
        {
            DesuscribirsePedidosEnVivo(); // Prevenimos colisiones de sockets anteriores

            // Asegurar que Supabase y su socket Realtime estén inicializados y conectados
            await InitializeAsync();

            // Escuchar TODOS los cambios (inserts, updates, deletes)
            // para que cancelaciones y cambios de estado se reflejen en cocina
            _canalPedidos = await Client.From<Pedido>().On(PostgresChangesOptions.ListenType.All, (sender, args) =>
            {
                var pedidoCambiado = args.Model<Pedido>();
                alRecibirCambio?.Invoke(pedidoCambiado);
            });
        }
    }
}