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
        // Solo pedimos el correo. El ID se genera en PostgreSQL y el Nombre no se guarda.
        public async Task<bool> InsertarPerfilAsync(string correo)
        {
            try
            {
                var nuevoPerfil = new UsuarioPerfil
                {
                    Email = correo,
                    EsAdmin = false // Por defecto, nadie es admin al registrarse
                };

                // Supabase solo enviará 'email' y 'es_admin' gracias al [JsonIgnore]
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
                // Esto ya no lanzará NullReferenceException
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
                // Traemos todos los pedidos, ordenados de forma descendente por fecha
                var respuesta = await _supabase.From<Pedido>()
                    .Order(p => p.Fecha, Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();

                // Retornamos la lista de modelos ya mapeados a C#
                return respuesta.Models;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener pedidos: {ex.Message}");
                return new List<Pedido>(); // Retornamos lista vacía si hay error para que no crashee
            }
        }
        public async Task<bool> CrearPedidoAsync(string clienteId, decimal totalFicticio)
        {
            try
            {
                var nuevoPedido = new Pedido
                {
                    ClienteId = clienteId,
                    Total = totalFicticio,
                    Estado = "En preparación",
                    Fecha = DateTime.UtcNow // Siempre es mejor guardar en UTC en bases de datos relacionales
                };

                var respuesta = await _supabase.From<Pedido>().Insert(nuevoPedido);

                return respuesta.Models.Count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear pedido: {ex.Message}");
                return false;
            }
        }
    }
}
