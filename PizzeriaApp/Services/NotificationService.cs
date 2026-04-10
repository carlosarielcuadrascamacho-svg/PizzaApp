using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Google.Apis.Auth.OAuth2;
using PizzeriaApp.Config;
using PizzeriaApp.Controllers;
using Microsoft.Maui.Storage;

namespace PizzeriaApp.Services
{
    /// <summary>
    /// Servicio estático para enviar Push Notifications vía Firebase Cloud Messaging (FCM) HTTP v1 API.
    /// NOTA: Esta implementación carga credenciales de la cuenta de servicio desde el cliente local,
    /// lo cual no se recomienda para producción, pero cumple la solicitud actual.
    /// </summary>
    public static class NotificationService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        
        private static GoogleCredential? _credential;

        /// <summary>
        /// Obtiene un Access Token generándolo a partir del archivo firebase-auth.json
        /// </summary>
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

        /// <summary>
        /// Envía una notificación push a un dispositivo específico usando la API HTTP v1.
        /// </summary>
        public static async Task<bool> EnviarNotificacionAsync(string token, string titulo, string cuerpo, Dictionary<string, string>? datosExtra = null)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine("FCM: Token del dispositivo vacío, no se puede enviar.");
                    return false;
                }

                if (string.IsNullOrEmpty(Secretos.FirebaseProjectId))
                {
                    Console.WriteLine("FCM: Firebase Project ID no configurado.");
                    return false;
                }

                string accessToken = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    Console.WriteLine("FCM: No se pudo obtener el Access Token.");
                    return false;
                }

                string fcmEndpoint = $"https://fcm.googleapis.com/v1/projects/{Secretos.FirebaseProjectId}/messages:send";

                var payload = new
                {
                    message = new
                    {
                        token = token,
                        notification = new
                        {
                            title = titulo,
                            body = cuerpo
                        },
                        android = new
                        {
                            notification = new
                            {
                                sound = "default",
                                click_action = "FCM_PLUGIN_ACTIVITY"
                            }
                        },
                        data = datosExtra ?? new Dictionary<string, string>()
                    }
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, fcmEndpoint);
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"FCM: Notificación enviada exitosamente al token: ...{token[^6..]}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"FCM Error: {response.StatusCode} — {responseBody}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FCM Exception: {ex.Message}");
                return false;
            }
        }

        public static async Task NotificarBienvenidaAsync(DataBaseServices db, string userId, string nombre, bool esAdmin)
        {
            try
            {
                var token = await db.ObtenerTokenPorClienteIdAsync(userId);
                if (string.IsNullOrEmpty(token)) return;

                string titulo = esAdmin ? "👨‍💼 ¡Bienvenido, Admin!" : $"🍕 ¡Bienvenido {nombre}!";
                string cuerpo = esAdmin ? "¡Que tenga muchas ventas hoy! 🍕🔥" : "Bienvenido a PizzaSteve, ¡revisa nuestro menú y haz tu pedido!";

                var datos = new Dictionary<string, string>
                {
                    { "tipo", "bienvenida" },
                    { "es_admin", esAdmin.ToString() }
                };

                await EnviarNotificacionAsync(token, titulo, cuerpo, datos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FCM Error: {ex.Message}");
            }
        }

        public static async Task NotificarPedidoRecibidoAClienteAsync(DataBaseServices db, string clienteId)
        {
            try
            {
                var token = await db.ObtenerTokenPorClienteIdAsync(clienteId);
                if (string.IsNullOrEmpty(token)) return;

                var datos = new Dictionary<string, string>
                {
                    { "tipo", "pedido_confirmado" },
                    { "timestamp", DateTime.UtcNow.ToString("o") }
                };

                await EnviarNotificacionAsync(token, "✅ ¡Pedido Recibido!", "Su pedido ha llegado a la sucursal correctamente. Estamos preparándolo.", datos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FCM Error: {ex.Message}");
            }
        }

        public static async Task NotificarCancelacionClienteAAdminsAsync(DataBaseServices db, string pedidoIdVisible, string nombreCliente)
        {
            try
            {
                var tokensAdmins = await db.ObtenerTokenAdminAsync();
                if (tokensAdmins == null || tokensAdmins.Count == 0) return;

                var datos = new Dictionary<string, string>
                {
                    { "tipo", "cancelacion_cliente" },
                    { "pedido_id", pedidoIdVisible },
                    { "timestamp", DateTime.UtcNow.ToString("o") }
                };

                foreach (var token in tokensAdmins)
                {
                    _ = EnviarNotificacionAsync(token, $"❌ Pedido {pedidoIdVisible} Cancelado", $"El cliente {nombreCliente} ha cancelado su pedido.", datos);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FCM Error: {ex.Message}");
            }
        }

        public static async Task NotificarNuevoPedidoAAdminsAsync(DataBaseServices db)
        {
            try
            {
                var tokensAdmins = await db.ObtenerTokenAdminAsync();
                if (tokensAdmins == null || tokensAdmins.Count == 0) return;

                var datos = new Dictionary<string, string>
                {
                    { "tipo", "nuevo_pedido" },
                    { "timestamp", DateTime.UtcNow.ToString("o") }
                };

                foreach (var token in tokensAdmins)
                {
                    _ = EnviarNotificacionAsync(token, "🍕 ¡Nuevo Pedido!", "Un cliente ha realizado un nuevo pedido. Revisa la cola de cocina.", datos);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FCM Error: {ex.Message}");
            }
        }

        public static async Task NotificarCambioEstadoAClienteAsync(DataBaseServices db, string clienteId, string nuevoEstado, string pedidoIdVisible)
        {
            try
            {
                var tokenCliente = await db.ObtenerTokenPorClienteIdAsync(clienteId);
                if (string.IsNullOrEmpty(tokenCliente)) return;

                string emoji = nuevoEstado switch
                {
                    "En preparación" => "👨‍🍳",
                    "Listo" => "✅",
                    "Entregado" => "🎉",
                    "Cancelado" => "❌",
                    _ => "📋"
                };

                var datos = new Dictionary<string, string>
                {
                    { "tipo", "cambio_estado" },
                    { "nuevo_estado", nuevoEstado },
                    { "pedido_id", pedidoIdVisible }
                };

                await EnviarNotificacionAsync(tokenCliente, $"{emoji} Pedido {pedidoIdVisible}", $"Tu pedido ahora está: {nuevoEstado}", datos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FCM Error: {ex.Message}");
            }
        }
    }
}