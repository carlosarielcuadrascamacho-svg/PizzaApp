using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Google.Apis.Auth.OAuth2;
using PizzeriaApp.Config;
using PizzeriaApp.Services;
using Microsoft.Maui.Storage;

namespace PizzeriaApp.Services
{
    /// <summary>
    /// Este servicio orquestra el envío de mensajes push hacia Firebase Cloud Messaging (FCM).
    /// </summary>
    public static class NotificationService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static GoogleCredential? _credential;

        private static async Task<string?> GetAccessTokenAsync()
        {
            try
            {
                if (_credential == null)
                {
                    using var stream = await FileSystem.OpenAppPackageFileAsync("firebase-auth.json");
                    _credential = GoogleCredential.FromStream(stream)
                                                  .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
                }
                var token = await _credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
                return token;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FCM: Error obteniendo Access Token: {ex.Message}");
                return null;
            }
        }

        public static async Task<bool> EnviarNotificacionAsync(string token, string titulo, string cuerpo, Dictionary<string, string>? datosExtra = null)
        {
            try
            {
                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(Secretos.FirebaseProjectId))
                    return false;

                string accessToken = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken)) return false;

                string fcmEndpoint = $"https://fcm.googleapis.com/v1/projects/{Secretos.FirebaseProjectId}/messages:send";

                var payload = new
                {
                    message = new
                    {
                        token = token,
                        notification = new { title = titulo, body = cuerpo },
                        android = new { notification = new { sound = "default", click_action = "FCM_PLUGIN_ACTIVITY" } },
                        data = datosExtra ?? new Dictionary<string, string>()
                    }
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, fcmEndpoint);
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FCM Exception: {ex.Message}");
                return false;
            }
        }

        public static async Task NotificarBienvenidaAsync(string userId, string nombre, bool esAdmin)
        {
            try
            {
                var perfiles = new ServicioPerfiles();
                var token = await perfiles.ObtenerFcmTokenAsync(userId);
                if (string.IsNullOrEmpty(token)) return;

                string titulo = esAdmin ? "👨‍💼 ¡Bienvenido, Admin!" : $"🍕 ¡Bienvenido {nombre}!";
                string cuerpo = esAdmin ? "¡Que tenga muchas ventas hoy! 🍕🔥" : "Bienvenido a PizzaSteve, ¡revisa nuestro menú y haz tu pedido!";

                var datos = new Dictionary<string, string> { { "tipo", "bienvenida" }, { "es_admin", esAdmin.ToString() } };
                await EnviarNotificacionAsync(token, titulo, cuerpo, datos);
            }
            catch (Exception ex) { Console.WriteLine($"FCM Error: {ex.Message}"); }
        }

        public static async Task NotificarPedidoRecibidoAClienteAsync(string clienteId)
        {
            try
            {
                var perfiles = new ServicioPerfiles();
                var token = await perfiles.ObtenerFcmTokenAsync(clienteId);
                if (string.IsNullOrEmpty(token)) return;

                var datos = new Dictionary<string, string> { { "tipo", "pedido_confirmado" } };
                await EnviarNotificacionAsync(token, "✅ ¡Pedido Recibido!", "Su pedido ha llegado correctamente. Estamos preparándolo.", datos);
            }
            catch (Exception ex) { Console.WriteLine($"FCM Error: {ex.Message}"); }
        }

        public static async Task NotificarCancelacionClienteAAdminsAsync(string pedidoIdVisible, string nombreCliente)
        {
            try
            {
                var perfiles = new ServicioPerfiles();
                var tokensAdmins = await perfiles.ObtenerTokensAdminsAsync();
                if (tokensAdmins == null) return;

                var datos = new Dictionary<string, string> { { "tipo", "cancelacion_cliente" }, { "pedido_id", pedidoIdVisible } };
                foreach (var token in tokensAdmins)
                    _ = EnviarNotificacionAsync(token, $"❌ Pedido {pedidoIdVisible} Cancelado", $"El cliente {nombreCliente} ha cancelado su pedido.", datos);
            }
            catch (Exception ex) { Console.WriteLine($"FCM Error: {ex.Message}"); }
        }

        public static async Task NotificarNuevoPedidoAAdminsAsync()
        {
            try
            {
                var perfiles = new ServicioPerfiles();
                var tokensAdmins = await perfiles.ObtenerTokensAdminsAsync();
                if (tokensAdmins == null) return;

                var datos = new Dictionary<string, string> { { "tipo", "nuevo_pedido" } };
                foreach (var token in tokensAdmins)
                    _ = EnviarNotificacionAsync(token, "🍕 ¡Nuevo Pedido!", "Se ha realizado un nuevo pedido. Revisa la cola de cocina.", datos);
            }
            catch (Exception ex) { Console.WriteLine($"FCM Error: {ex.Message}"); }
        }

        public static async Task NotificarCambioEstadoAClienteAsync(string clienteId, string nuevoEstado, string pedidoIdVisible)
        {
            try
            {
                var perfiles = new ServicioPerfiles();
                var tokenCliente = await perfiles.ObtenerFcmTokenAsync(clienteId);
                if (string.IsNullOrEmpty(tokenCliente)) return;

                string emoji = nuevoEstado switch { "En preparación" => "👨‍🍳", "Listo" => "✅", "Entregado" => "🎉", "Cancelado" => "❌", _ => "📋" };
                var datos = new Dictionary<string, string> { { "tipo", "cambio_estado" }, { "nuevo_estado", nuevoEstado }, { "pedido_id", pedidoIdVisible } };

                await EnviarNotificacionAsync(tokenCliente, $"{emoji} Pedido {pedidoIdVisible}", $"Tu pedido ahora está: {nuevoEstado}", datos);
            }
            catch (Exception ex) { Console.WriteLine($"FCM Error: {ex.Message}"); }
        }
    }
}