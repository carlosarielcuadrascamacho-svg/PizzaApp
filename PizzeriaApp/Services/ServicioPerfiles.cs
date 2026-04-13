using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PizzeriaApp.Models;

namespace PizzeriaApp.Services
{
    /// <summary>
    /// Gestiona todo lo relacionado con perfiles de usuario, roles y tokens de notificación.
    /// </summary>
    public class ServicioPerfiles : ServicioBase
    {
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
                Console.WriteLine($"[ServicioPerfiles] Error al insertar perfil: {ex.Message}");
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
                Console.WriteLine($"[ServicioPerfiles] Error obteniendo perfil: {ex.Message}");
                return null;
            }
        }

        public async Task<string?> ObtenerIdPorEmailAsync(string correo)
        {
            try
            {
                var resultado = await Client.From<UsuarioPerfil>().Where(p => p.Email == correo).Get();
                return resultado.Models.FirstOrDefault()?.Id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServicioPerfiles] Error obteniendo ID por email: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> EsUsuarioAdminAsync(string userId)
        {
            try
            {
                var perfil = await ObtenerPerfilAsync(userId);
                return perfil?.EsAdmin ?? false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServicioPerfiles] Error verificando admin: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ActualizarPerfilAsync(string id, string nombre, string direccion, string telefono, string fotoBase64)
        {
            try
            {
                var res = await Client.From<UsuarioPerfil>()
                    .Where(p => p.Id == id)
                    .Set(p => p.Nombre, nombre)
                    .Set(p => p.Direccion, direccion)
                    .Set(p => p.Telefono, telefono)
                    .Set(p => p.FotoPerfil, fotoBase64)
                    .Update();
                return res.Models.Count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServicioPerfiles] Error actualizando perfil: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> GuardarFcmTokenAsync(string userId, string token)
        {
            try
            {
                var res = await Client.From<UsuarioPerfil>()
                    .Where(p => p.Id == userId)
                    .Set(p => p.FcmToken, token)
                    .Update();
                return res.Models.Count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServicioPerfiles] Error guardando FCM token: {ex.Message}");
                return false;
            }
        }

        public async Task<string?> ObtenerFcmTokenAsync(string userId)
        {
            try
            {
                var res = await Client.From<UsuarioPerfil>().Where(p => p.Id == userId).Get();
                return res.Models.FirstOrDefault()?.FcmToken;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServicioPerfiles] Error obteniendo FCM token: {ex.Message}");
                return null;
            }
        }

        public async Task<List<string>> ObtenerTokensAdminsAsync()
        {
            try
            {
                var res = await Client.From<UsuarioPerfil>().Where(p => p.EsAdmin == true).Get();
                return res.Models
                    .Where(p => !string.IsNullOrEmpty(p.FcmToken))
                    .Select(p => p.FcmToken!)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServicioPerfiles] Error obteniendo tokens admin: {ex.Message}");
                return new List<string>();
            }
        }
    }
}
