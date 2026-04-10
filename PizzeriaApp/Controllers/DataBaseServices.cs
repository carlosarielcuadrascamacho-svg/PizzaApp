using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PizzeriaApp.Models;
using Supabase;
using PizzeriaApp.Config; // Aquí es donde guardamos las llaves y la URL de Supabase para no quemarlas en el código
using Supabase.Realtime;
using Supabase.Realtime.PostgresChanges;
using PizzeriaApp.Services;

namespace PizzeriaApp.Controllers
{
    // Esta clase es el corazón de los datos. Centralizamos todo lo que sea pegarle a Supabase aquí.
    public class DataBaseServices
    {
        // El cliente de Supabase. Lo marcamos como nulable porque se inicializa después.
        private Supabase.Client? _supabase;

        // Propiedad para acceder al cliente de forma segura. Si no está listo, truena con una excepción clara.
        private Supabase.Client Client => _supabase ?? SupabaseClientFactory.GetClientOrThrow();
        
        // Bandera simple para no reinicializar todo a cada rato
        private bool _initialized = false;

        public DataBaseServices()
        {
            // Intentamos agarrar el cliente que ya viva en el Factory (patrón Singleton para no crear mil conexiones)
            _supabase = SupabaseClientFactory.Client;
        }

        // Este método asegura que la conexión a Supabase esté arriba antes de hacer cualquier consulta
        public async Task InitializeAsync()
        {
            // Si ya estamos listos, ni le movemos
            if (_initialized)
                return;

            try
            {
                // Mandamos llamar al Factory con las credenciales que tenemos en 'Secretos'
                await SupabaseClientFactory.InitializeAsync(Secretos.SupabaseUrl, Secretos.SupabaseApiKey);
                
                // Refrescamos nuestra referencia local al cliente único
                _supabase = SupabaseClientFactory.Client;
                _initialized = true;
            }
            catch (Exception ex)
            {
                // Si esto falla, la app básicamente no sirve, así que logueamos y relanzamos para que lo cachen arriba
                Console.WriteLine($"Error inicializando Supabase desde DataBaseServices: {ex.Message}");
                throw;
            }
        }

        // Registramos un nuevo perfil de usuario cuando se dan de alta en la app
        public async Task<bool> InsertarPerfilAsync(string correo)
        {
            try
            {
                // Por defecto los nuevos no son admins, hay que cuidarnos de eso
                var nuevoPerfil = new UsuarioPerfil { Email = correo, EsAdmin = false };
                
                // Metemos los datos a la tabla 'perfiles' (mapeada en UsuarioPerfil)
                var respuesta = await Client.From<UsuarioPerfil>().Insert(nuevoPerfil);
                
                // Si Supabase nos devuelve el modelo insertado, es que todo salió bien
                return respuesta.Models.Count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }

        // Jalamos toda la info de un perfil usando su ID (el de Auth de Supabase)
        public async Task<UsuarioPerfil?> ObtenerPerfilAsync(string idUsuario)
        {
            try
            {
                // Filtramos por ID y pedimos el primer resultado que encuentre
                var resultado = await Client.From<UsuarioPerfil>().Where(p => p.Id == idUsuario).Get();
                return resultado.Models.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo perfil completo: {ex.Message}");
                return null;
            }
        }

        // Método rápido para buscar el ID de alguien solo con su correo
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

        // Checamos si el usuario tiene permisos de jefe para mostrarle el panel de admin
        public async Task<bool> EsUsuarioAdminAsync(string IdBusqueda)
        {
            try
            {
                var resultado = await Client.From<UsuarioPerfil>().Where(p => p.Id == IdBusqueda).Get();
                // Si el campo EsAdmin viene null, asumimos que no es admin por seguridad
                return resultado.Models.FirstOrDefault()?.EsAdmin ?? false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }

        // Traemos solo las pizzas y complementos que sí están disponibles para el público
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

        // Este método guarda un pedido. Es de los más importantes: primero el Pedido y luego sus detalles.
        public async Task<bool> CrearPedidoCompletoAsync(string clienteId, List<ItemCarrito> carrito, decimal totalCalculado, string estadoInicial)
        {
            try
            {
                var nuevoPedido = new Pedido
                {
                    ClienteId = clienteId,
                    Total = totalCalculado,
                    Estado = estadoInicial,
                    Fecha = DateTime.UtcNow // Siempre guardamos en UTC para no tener líos de zonas horarias
                };

                // Insertamos la cabecera del pedido primero para generar su ID
                var respuestaPedido = await Client.From<Pedido>().Insert(nuevoPedido);
                var pedidoInsertado = respuestaPedido.Models.FirstOrDefault();

                if (pedidoInsertado == null) return false;

                // Ahora preparamos los detalles (qué productos compró y a qué precio)
                var detalles = new List<DetallePedido>();
                foreach (var item in carrito)
                {
                    detalles.Add(new DetallePedido
                    {
                        PedidoId = pedidoInsertado.Id, // El ID que acabamos de generar arriba
                        ProductoId = item.Producto.Id,
                        Cantidad = item.Cantidad,
                        PrecioUnitario = item.Producto.Precio
                    });
                }

                // Subimos todos los detalles de un solo golpe (bulk insert)
                await Client.From<DetallePedido>().Insert(detalles);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en transacción: {ex.Message}");
                return false;
            }
        }

        // Versión mejorada del pedido. Incluye mesa y comentarios (por si quieren la pizza sin orillas, etc.)
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

        // Agregamos una nueva pizza al catálogo de la base de datos
        public async Task<bool> InsertarProductoAsync(Producto nuevoProducto)
        {
            try
            {
                // Se guarda en la tabla 'productos' según definimos en el Modelo
                var respuesta = await Client.From<Producto>().Insert(nuevoProducto);
                return respuesta.Models.Count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al insertar producto: {ex.Message}");
                return false;
            }
        }

        // Editamos los datos de una pizza que ya existe
        public async Task<bool> ActualizarProductoAsync(Producto p)
        {
            try
            {
                // Hacemos el update especificando campo por campo para no sobrescribir algo por error
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

        // Obtenemos los pedidos que la cocina todavía tiene que preparar
        public async Task<List<Pedido>> ObtenerPedidosActivosAsync()
        {
            try
            {
                // Solo traemos pedidos que no han sido entregados ni cancelados
                var respuesta = await Client.From<Pedido>()
                    .Where(p => p.Estado != "Entregado" && p.Estado != "Cancelado")
                    .Order(p => p.Fecha, Supabase.Postgrest.Constants.Ordering.Ascending) // Los más viejos primero para despacharlos en orden
                    .Get();

                var pedidosActivos = respuesta.Models;
                var idsPedidos = pedidosActivos.Select(p => p.Id).ToList();

                if (idsPedidos.Any())
                {
                    // Como Supabase no hace Joins complejos fácil, traemos detalles y productos para armar el modelo en memoria
                    // Lanzamos ambas peticiones en paralelo para que la app no se sienta lenta
                    var detallesTask = Client.From<DetallePedido>()
                        .Filter("pedido_id", Supabase.Postgrest.Constants.Operator.In, idsPedidos)
                        .Get();
                    var productosTask = Client.From<Producto>().Get();

                    await Task.WhenAll(detallesTask, productosTask);

                    var detallesResponse = detallesTask.Result;
                    // Creamos un diccionario de productos para buscar el nombre rápido por su ID
                    var productosMapa = productosTask.Result.Models.ToDictionary(p => p.Id, p => p.Nombre);

                    foreach (var pedido in pedidosActivos)
                    {
                        // Filtramos los detalles de este pedido específico
                        var detallesDelPedido = detallesResponse.Models.Where(d => d.PedidoId == pedido.Id).ToList();
                        foreach (var d in detallesDelPedido)
                        {
                            // Le pegamos el nombre del platillo para que en la lista se vea \"Pizza Peperoni\" y no solo un ID feo
                            d.NombrePlatillo = productosMapa.ContainsKey(d.ProductoId) ? productosMapa[d.ProductoId] : \"Indefinido\";
                        }
                        // Usamos ObservableCollection para que el UI se refresque solito si añadimos algo (aunque aquí es carga inicial)
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

        // Historial administrativo de todo lo que ya se cobró y entregó
        public async Task<List<Pedido>> ObtenerPedidosCompletadosAsync()
        {
            try
            {
                var respuesta = await Client.From<Pedido>()
                    .Where(p => p.Estado == \"Entregado\")
                    .Order(p => p.Fecha, Supabase.Postgrest.Constants.Ordering.Descending) // Los más recientes arriba
                    .Get();

                return respuesta.Models;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener pedidos completados: {ex.Message}");
                return new List<Pedido>();
            }
        }

        // Cambiamos el estado de un pedido (ej: \"En Camino\" -> \"Entregado\")
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

        // Actualizamos los datos personales del cliente, incluyendo su foto en Base64
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

        // Habilitamos o deshabilitamos un producto del menú
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

        // Traemos todos los pedidos que ha hecho un cliente en específico para su vista de historial
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
                    // Igual que en cocina, armamos los detalles para que el cliente vea qué pidió exactamente cada vez
                    var detallesResponse = await Client.From<DetallePedido>()
                        .Filter(\"pedido_id\", Supabase.Postgrest.Constants.Operator.In, idsPedidos)
                        .Get();
                    
                    var productosResponse = await Client.From<Producto>().Get();
                    var productosMapa = productosResponse.Models.ToDictionary(p => p.Id, p => p.Nombre);

                    foreach (var pedido in historial)
                    {
                        var detallesDelPedido = detallesResponse.Models.Where(d => d.PedidoId == pedido.Id).ToList();
                        foreach (var d in detallesDelPedido)
                        {
                            d.NombrePlatillo = productosMapa.ContainsKey(d.ProductoId) ? productosMapa[d.ProductoId] : \"??\";
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

        // Trae absolutamente todas las pizzas, activas o no (para el panel del administrador)
        public async Task<List<Producto>> ObtenerCatalogoCompletoAsync()
        {
            try
            {
                var respuesta = await Client.From<Producto>().Get();
                return respuesta.Models;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo catálogo completo: {ex.Message}");
                return new List<Producto>();
            }
        }

        // Calculamos cuánto dinero ha entrado y qué es lo que más se vende
        public async Task<(decimal IngresosTotales, Dictionary<string, int> PlatillosPopulares)> ObtenerMetricasRealesAsync()
        {
            decimal totalCaja = 0;
            var platillosRanking = new Dictionary<string, int>();

            try
            {
                // Solo nos importan los pedidos que ya se pagaron y se entregaron
                var todosLosPedidos = await Client.From<Pedido>()
                    .Where(p => p.Estado == \"Entregado\")
                    .Get();

                var pedidos = todosLosPedidos.Models;
                totalCaja = pedidos.Sum(p => p.Total);

                var idsPedidos = pedidos.Select(p => p.Id).ToList();

                if (idsPedidos.Any())
                {
                    // Filtramos detalles por esos pedidos. PostgreSQL se encarga de procesar el \"IN\"
                    var detallesResponse = await Client.From<DetallePedido>()
                        .Filter(\"pedido_id\", Supabase.Postgrest.Constants.Operator.In, idsPedidos)
                        .Get();
                    
                    var detallesValidos = detallesResponse.Models;

                    // Mapeamos a nombres reales de pizza para que la gráfica de admin sea legible
                    var productosResponse = await Client.From<Producto>().Get();
                    var productosMapa = productosResponse.Models.ToDictionary(p => p.Id, p => p.Nombre);

                    foreach (var det in detallesValidos)
                    {
                        var nombreReal = productosMapa.ContainsKey(det.ProductoId) ? productosMapa[det.ProductoId] : \"Indefinido\";
                        if (!platillosRanking.ContainsKey(nombreReal))
                            platillosRanking[nombreReal] = 0;
                        
                        // Vamos sumando las unidades vendidas por producto
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

        // Limpieza del socket de tiempo real para no dejar conexiones colgadas
        public void DesuscribirsePedidosEnVivo()
        {
            if (_canalPedidos != null)
            {
                _canalPedidos.Unsubscribe();
                _canalPedidos = null;
            }
        }

        // Magia para que la cocina reciba el pedido al instante en que el cliente le pica a \"Pagar\"
        public async Task SuscribirseAPedidosEnVivo(Action<Pedido> alRecibirCambio)
        {
            DesuscribirsePedidosEnVivo(); // Matamos cualquier conexión previa para no duplicar eventos

            // Nos aseguramos que Supabase esté conectado
            await InitializeAsync();

            // Escuchamos TODO: Altas de nuevos pedidos, cambios de estado o si alguien cancela algo
            // El callback se lanza cada que la tabla 'pedidos' cambia en Supabase
            _canalPedidos = await Client.From<Pedido>().On(PostgresChangesOptions.ListenType.All, (sender, args) =>
            {
                var pedidoCambiado = args.Model<Pedido>();
                alRecibirCambio?.Invoke(pedidoCambiado); // Le avisamos a la vista que hay algo nuevo que mostrar
            });
        }

        // =============================================
        // ===  FCM Token Management (Push Notifications)
        // =============================================

        // Registramos el ID de Firebase del dispositivo del usuario. Lo necesitamos para mandarle alertas.
        public async Task<bool> GuardarFcmTokenAsync(string userId, string fcmToken)
        {
            try
            {
                // Lo guardamos directo en el perfil del usuario
                var actualizacion = await Client.From<UsuarioPerfil>()
                    .Where(p => p.Id == userId)
                    .Set(p => p.FcmToken, fcmToken)
                    .Update();

                return actualizacion.Models.Count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error guardando FCM token: {ex.Message}");
                return false;
            }
        }

        // Sacamos el token de un cliente para despertarlo con una notificación cuando su pizza esté lista
        public async Task<string?> ObtenerFcmTokenAsync(string userId)
        {
            try
            {
                var resultado = await Client.From<UsuarioPerfil>()
                    .Where(p => p.Id == userId)
                    .Get();

                return resultado.Models.FirstOrDefault()?.FcmToken;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo FCM token: {ex.Message}");
                return null;
            }
        }

        // Alias por si lo buscamos por \"ClienteId\" para que el código se lea más natural
        public async Task<string?> ObtenerTokenPorClienteIdAsync(string clienteId)
        {
            return await ObtenerFcmTokenAsync(clienteId);
        }

        // Buscamos a todos los que tengan la bandera 'EsAdmin' para mandarles el pitazo de que cayó un pedido nuevo
        public async Task<List<string>> ObtenerAdminsTokensAsync()
        {
            try
            {
                var resultado = await Client.From<UsuarioPerfil>()
                    .Where(p => p.EsAdmin == true)
                    .Get();

                // Filtramos a los que sí tengan un token válido registrado
                return resultado.Models
                    .Where(p => !string.IsNullOrEmpty(p.FcmToken))
                    .Select(p => p.FcmToken)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo tokens de admins: {ex.Message}");
                return new List<string>();
            }
        }

        // Alias para facilitar la lectura en el servicio de notificaciones
        public async Task<List<string>> ObtenerTokenAdminAsync()
        {
            return await ObtenerAdminsTokensAsync();
        }
    }
}