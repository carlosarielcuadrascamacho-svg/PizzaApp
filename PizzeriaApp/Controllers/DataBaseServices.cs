using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PizzeriaApp.Models;
using Supabase;
using PizzeriaApp.Config;

namespace PizzeriaApp.Controllers
{
    public class DataBaseServices
    {
        private readonly Supabase.Client _supabase;

        public DataBaseServices()
        {
            var options = new SupabaseOptions { AutoConnectRealtime = true };
            _supabase = new Client(Secretos.SupabaseUrl, Secretos.SupabaseApiKey, options);
        }

        public async Task<bool> InsertarPerfilAsync(string correo)
        {
            try
            {
                var nuevoPerfil = new UsuarioPerfil
                {
                    Email = correo,
                    EsAdmin = false
                };

                var respuesta = await _supabase.From<UsuarioPerfil>().Insert(nuevoPerfil);
                return respuesta.Models.Count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al insertar: {ex.Message}");
                return false;
            }
        }

        public async Task<string> ObtenerIdPorEmailAsync(string correoBusqueda)
        {
            try
            {
                var resultado = await _supabase
                    .From<UsuarioPerfil>()
                    .Where(p => p.Email == correoBusqueda)
                    .Get();

                var perfil = resultado.Models.FirstOrDefault();

                if (perfil != null)
                {
                    return perfil.Id;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al consultar ID: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> EsUsuarioAdminAsync(string IdBusqueda)
        {
            try
            {
                var resultado = await _supabase
                    .From<UsuarioPerfil>()
                    .Where(p => p.Id == IdBusqueda)
                    .Get();

                var perfil = resultado.Models.FirstOrDefault();

                if (perfil != null)
                {
                    return perfil.EsAdmin;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al verificar permisos: {ex.Message}");
                return false;
            }
        }

        public async Task<List<Pedido>> ObtenerPedidosActivosAsync()
        {
            try
            {
                var respuesta = await _supabase.From<Pedido>()
                    .Order(p => p.Fecha, Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();

                return respuesta.Models;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener pedidos: {ex.Message}");
                return new List<Pedido>();
            }
        }

        public async Task<List<Producto>> ObtenerProductosActivosAsync()
        {
            try
            {
                var respuesta = await _supabase.From<Producto>()
                    .Where(p => p.Activo == true)
                    .Get();

                return respuesta.Models;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener catálogo: {ex.Message}");
                return new List<Producto>();
            }
        }

        public async Task<bool> CrearPedidoCompletoAsync(string clienteId, List<ItemCarrito> carrito, decimal totalCalculado)
        {
            try
            {
                var nuevoPedido = new Pedido
                {
                    ClienteId = clienteId,
                    Total = totalCalculado,
                    Estado = "En preparación",
                    Fecha = DateTime.UtcNow
                };

                var respuestaPedido = await _supabase.From<Pedido>().Insert(nuevoPedido);
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

                await _supabase.From<DetallePedido>().Insert(detalles);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en transacción de pedido: {ex.Message}");
                return false;
            }
        }
    }
}