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
        public async Task<bool> InsertarPerfilAsync(string nuevoId, string correo)
        {
            try
            {
                bool isAdmin = false;
                var nuevoPerfil = new UsuarioPerfil
                {
                    Id = nuevoId,
                    Email = correo,
                    EsAdmin = isAdmin
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
    }
}
