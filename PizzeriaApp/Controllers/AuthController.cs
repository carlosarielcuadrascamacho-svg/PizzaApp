using System;
using System.Threading.Tasks;
using PizzeriaApp.Models;
using PizzeriaApp.Services;
using PizzeriaApp.GoogleAuth;

namespace PizzeriaApp.Controllers
{
    // El Controlador de Autenticación coordina el flujo entre Google y nuestra base de datos
    public class AuthController
    {
        private readonly IGoogleAuthService _googleAuth;
        private readonly DataBaseServices _db;

        public AuthController(IGoogleAuthService googleAuth, DataBaseServices db)
        {
            _googleAuth = googleAuth;
            _db = db;
        }

        // Orquesta el proceso de login completo
        public async Task<UsuarioPerfil?> LoginConGoogleAsync()
        {
            // 1. Handshake con Google
            var loggedUser = await _googleAuth.GetCurrentUserAsync() ?? await _googleAuth.AuthenticateAsync();
            if (loggedUser == null) return null;

            // 2. Vinculación con nuestra base de datos
            string idUser = await _db.ObtenerIdPorEmailAsync(loggedUser.Email);

            if (string.IsNullOrEmpty(idUser))
            {
                await _db.InsertarPerfilAsync(loggedUser.Email);
                idUser = await _db.ObtenerIdPorEmailAsync(loggedUser.Email);
            }

            // 3. Verificación de roles
            bool esAdmin = await _db.EsUsuarioAdminAsync(idUser);

            // 4. Construcción del perfil de sesión
            string nombreUsuario = !string.IsNullOrWhiteSpace(loggedUser.FullName)
                                    ? loggedUser.FullName
                                    : loggedUser.Email.Split('@')[0];

            return new UsuarioPerfil
            {
                Id = idUser,
                Email = loggedUser.Email,
                EsAdmin = esAdmin,
                Nombre = nombreUsuario
            };
        }

        // Actualiza los datos del perfil del usuario
        public async Task<bool> ActualizarPerfilUsuarioAsync(string id, string nombre, string direccion, string telefono, string fotoBase64)
        {
            return await _db.ActualizarPerfilAsync(id, nombre, direccion, telefono, fotoBase64);
        }

        // Obtiene el perfil completo del usuario
        public async Task<UsuarioPerfil?> ObtenerPerfilAsync(string userId)
        {
            return await _db.ObtenerPerfilAsync(userId);
        }

        // Registra el token para notificaciones
        public async Task GuardarTokenDispositivoAsync(string userId, string token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                await _db.GuardarFcmTokenAsync(userId, token);
            }
        }
    }
}
