using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using PizzeriaApp.Models;
using Supabase;

namespace PizzeriaApp.Data
{
    public class DataBaseServices
    {
        private readonly Supabase.Client _supabase; //Instancia del cliente de Supabase para realizar las operaciones de base de datos relacionadas con los perfiles de usuario
        public DataBaseServices(Supabase.Client supabase) //Constructor que recibe una instancia del cliente de Supabase para inicializar el servicio de base de datos
        {
            _supabase = supabase;
        }
        public async Task<bool> InsertarPerfilAsync(string nuevoId, string correo) // Método asíncrono para insertar un nuevo perfil de usuario en la base de datos, recibe el nuevo ID y el correo electrónico como parámetros
        {
            try
            {
                bool isAdmin = false;// Por defecto, el nuevo perfil no es admin
                var nuevoPerfil = new UsuarioPerfil// Creamos una nueva instancia de UsuarioPerfil con los datos proporcionados
                {
                    Id = nuevoId,
                    Email = correo,
                    EsAdmin = isAdmin
                };

                // Realiza la inserción
                var respuesta = await _supabase.From<UsuarioPerfil>().Insert(nuevoPerfil);

                return respuesta.Models.Count > 0;// Si la inserción fue exitosa, el número de modelos devueltos será mayor que 0
            }
            catch (Exception ex)
            {
                throw new Exception($"Fallo en la base de datos al registrar el usuario: {ex.Message}");
            }
        }

        public async Task<string> ObtenerIdPorEmailAsync(string correoBusqueda) // Método asíncrono para obtener el ID de un perfil de usuario a partir de su correo electrónico, recibe el correo electrónico como parámetro
        {
            try
            {
                // Realizamos la consulta filtrando por la columna 'email'
                var resultado = await _supabase
                    .From<UsuarioPerfil>()
                    .Where(p => p.Email == correoBusqueda)
                    .Get();

                
                var perfil = resultado.Models.FirstOrDefault(); // Extraemos el primer modelo encontrado

                if (perfil != null)
                {
                    return perfil.Id; // Retorna el UUID encontrado
                }

                return null; // Si no existe el perfil
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al consultar ID: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> EsUsuarioAdminAsync(string IdBusqueda) // Método asíncrono para verificar si un usuario es admin a partir de su ID, recibe el ID del usuario como parámetro
        {
            try
            {
                // Realizamos la consulta filtrando por el id del usuario
                var resultado = await _supabase
                    .From<UsuarioPerfil>()
                    .Where(p => p.Id == IdBusqueda)
                    .Get();

                // Obtenemos el primer registro que coincida
                var perfil = resultado.Models.FirstOrDefault();

                // Si el perfil existe, devolvemos el valor de la columna 'es_admin'
                if (perfil != null)
                {
                    return perfil.EsAdmin;
                }

                // Si no se encuentra el perfil, asumimos que no es admin
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
