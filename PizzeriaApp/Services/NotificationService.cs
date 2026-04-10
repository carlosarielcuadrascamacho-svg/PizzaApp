using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PizzeriaApp.Config;
using PizzeriaApp.Controllers;

namespace PizzeriaApp.Services
{
    /// <summary>
    /// Servicio estático para enviar Push Notifications vía Firebase Cloud Messaging (FCM).
    /// Utiliza la API legacy de FCM con Server Key.
    /// </summary>
    public static class NotificationService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string FcmEndpoint = "https://fcm.googleapis.com/fcm/send";

        /// <summary>
        /// Envía una notificación push a un dispositivo específico.
        /// </summary>
        /// <param name="token">Token FCM del dispositivo destino</param>
        /// <param name="titulo">Título de la notificación</param>
        /// <param name="cuerpo">Cuerpo/mensaje de la notificación</param>
        /// <param name="datosExtra">Datos adicionales opcionales (key-value) para la app</param>
        public static async Task<bool> EnviarNotificacionAsync(string token, string titulo, string cuerpo, Dictionary<string, string>? datosExtra = null)
        {
            try
            {
                if (string.IsNullOrEmpty(token) || Secretos.FcmServerKey == "TU_SERVER_KEY_AQUI")
                {
                    Console.WriteLine("FCM: Token vacío o Server Key no configurada. Notificación omitida.");
                    return false;
                }

                var payload = new
                {
                    to = token,
                    notification = new
                    {
                        title = titulo,
                        body = cuerpo,
                        sound = "default",
                        click_action = "FCM_PLUGIN_ACTIVITY"
                    },
                    data = datosExtra ?? new Dictionary<string, string>()
                };

                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, FcmEndpoint);
                request.Headers.TryAddWithoutValidation("Authorization", $"key={Secretos.FcmServerKey}");
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"FCM: Notificación enviada exitosamente a token: ...{token[^6..]}");
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

        /// <summary>
        /// Notifica a TODOS los administradores que un cliente ha realizado un nuevo pedido.
        /// Se ejecuta en fire-and-forget para no bloquear la UX del cliente.
        /// </summary>
        public static async Task NotificarNuevoPedidoAAdminsAsync(DataBaseServices db)
        {
            try
            {
                var tokensAdmins = await db.ObtenerAdminsTokensAsync();

                if (tokensAdmins == null || tokensAdmins.Count == 0)
                {
                    Console.WriteLine("FCM: No hay admins con token FCM registrado.");
                    return;
                }

                var datos = new Dictionary<string, string>
                {
                    { "tipo", "nuevo_pedido" },
                    { "timestamp", DateTime.UtcNow.ToString("o") }
                };

                foreach (var token in tokensAdmins)
                {
                    // Fire-and-forget cada envío individual (no esperar entre notificaciones)
                    _ = EnviarNotificacionAsync(
                        token,
                        "🍕 ¡Nuevo Pedido!",
                        "Un cliente ha realizado un nuevo pedido. Revisa la cola de cocina.",
                        datos
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FCM: Error notificando admins: {ex.Message}");
            }
        }

        /// <summary>
        /// Notifica a un cliente que el estado de su pedido ha cambiado.
        /// </summary>
        /// <param name="db">Servicio de base de datos</param>
        /// <param name="clienteId">ID del cliente dueño del pedido</param>
        /// <param name="nuevoEstado">Nuevo estado del pedido</param>
        /// <param name="pedidoIdVisible">ID visible del pedido (ej. #A1B2C3)</param>
        public static async Task NotificarCambioEstadoAClienteAsync(DataBaseServices db, string clienteId, string nuevoEstado, string pedidoIdVisible)
        {
            try
            {
                var tokenCliente = await db.ObtenerFcmTokenAsync(clienteId);

                if (string.IsNullOrEmpty(tokenCliente))
                {
                    Console.WriteLine($"FCM: Cliente {clienteId} no tiene token FCM registrado.");
                    return;
                }

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

                await EnviarNotificacionAsync(
                    tokenCliente,
                    $"{emoji} Pedido {pedidoIdVisible}",
                    $"Tu pedido ahora está: {nuevoEstado}",
                    datos
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FCM: Error notificando cliente: {ex.Message}");
            }
        }

        /// <summary>
        /// Envía una notificación de bienvenida al usuario que acaba de iniciar sesión.
        /// Cliente: "Bienvenido {nombre} a PizzaSteve"
        /// Admin: "Bienvenido, ¡que tenga muchas ventas hoy!"
        /// </summary>
        public static async Task NotificarBienvenidaAsync(DataBaseServices db, string userId, string nombre, bool esAdmin)
        {
            try
            {
                var token = await db.ObtenerFcmTokenAsync(userId);
                if (string.IsNullOrEmpty(token)) return;

                string titulo;
                string cuerpo;

                if (esAdmin)
                {
                    titulo = "👨‍💼 ¡Bienvenido, Admin!";
                    cuerpo = "¡Que tenga muchas ventas hoy! 🍕🔥";
                }
                else
                {
                    titulo = $"🍕 ¡Bienvenido {nombre}!";
                    cuerpo = "Bienvenido a PizzaSteve, ¡revisa nuestro menú y haz tu pedido!";
                }

                var datos = new Dictionary<string, string>
                {
                    { "tipo", "bienvenida" },
                    { "es_admin", esAdmin.ToString() }
                };

                await EnviarNotificacionAsync(token, titulo, cuerpo, datos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FCM: Error enviando bienvenida: {ex.Message}");
            }
        }

        /// <summary>
        /// Notifica al cliente que su pedido ha llegado a la sucursal correctamente.
        /// Se llama después de crear un pedido exitosamente.
        /// </summary>
        public static async Task NotificarPedidoRecibidoAClienteAsync(DataBaseServices db, string clienteId)
        {
            try
            {
                var token = await db.ObtenerFcmTokenAsync(clienteId);
                if (string.IsNullOrEmpty(token)) return;

                var datos = new Dictionary<string, string>
                {
                    { "tipo", "pedido_confirmado" },
                    { "timestamp", DateTime.UtcNow.ToString("o") }
                };

                await EnviarNotificacionAsync(
                    token,
                    "✅ ¡Pedido Recibido!",
                    "Su pedido ha llegado a la sucursal correctamente. Estamos preparándolo.",
                    datos
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FCM: Error notificando pedido recibido: {ex.Message}");
            }
        }

        /// <summary>
        /// Notifica a los administradores que un CLIENTE ha cancelado su propio pedido.
        /// Se llama desde HistorialCliente cuando el cliente presiona "Cancelar".
        /// </summary>
        public static async Task NotificarCancelacionClienteAAdminsAsync(DataBaseServices db, string pedidoIdVisible, string nombreCliente)
        {
            try
            {
                var tokensAdmins = await db.ObtenerAdminsTokensAsync();
                if (tokensAdmins == null || tokensAdmins.Count == 0) return;

                var datos = new Dictionary<string, string>
                {
                    { "tipo", "cancelacion_cliente" },
                    { "pedido_id", pedidoIdVisible },
                    { "timestamp", DateTime.UtcNow.ToString("o") }
                };

                foreach (var token in tokensAdmins)
                {
                    _ = EnviarNotificacionAsync(
                        token,
                        $"❌ Pedido {pedidoIdVisible} Cancelado",
                        $"El cliente {nombreCliente} ha cancelado su pedido.",
                        datos
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FCM: Error notificando cancelación a admins: {ex.Message}");
            }
        }
    }
}
