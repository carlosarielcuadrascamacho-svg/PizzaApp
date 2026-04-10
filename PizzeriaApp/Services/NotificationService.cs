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
    // Este servicio es el que orquestra el envío de mensajes push hacia Google
    // Usamos la API v1 de Firebase, que es la más moderna y segura actualmente
    public static class NotificationService
    {
        // Reutilizamos el HttpClient para no saturar los sockets del dispositivo
        private static readonly HttpClient _httpClient = new HttpClient();
        
        // Aquí guardaremos las credenciales de Google una vez cargadas para no leer el archivo cada vez
        private static GoogleCredential? _credential;

        // Generamos el Access Token necesario para hablar con Google; dura una hora y se autogestiona
        private static async Task<string?> GetAccessTokenAsync()
        {
            try
            {
                if (_credential == null)
                {
                    // Abrimos el archivo de credenciales que incluimos en el paquete de la app
                    using var stream = await FileSystem.OpenAppPackageFileAsync("firebase-auth.json");
                    // Cargamos la cuenta de servicio y le damos permisos específicos para mensajería
                    _credential = GoogleCredential.FromStream(stream)
                                                     .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
                }
                
                // Pedimos el token actual; si expiró, la librería de Google lo refresca solita
                var token = await _credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
                return token;
            }
            catch (Exception ex)
            {
                // Si el archivo no está o la llave está mal, aquí es donde lo sabremos
                Console.WriteLine($"FCM: Error obteniendo Access Token: {ex.Message}");
                return null;
            }
        }

        // El caballito de batalla: envía una notificación a un token específico usando el formato HTTP v1
        public static async Task<bool> EnviarNotificacionAsync(string token, string titulo, string cuerpo, Dictionary<string, string>? datosExtra = null)
        {
            try
            {
                // Unas validaciones rápidas para no hacer peticiones que van a fallar seguro
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

                // Necesitamos el token OAuth2 antes de intentar enviar el mensaje
                string accessToken = await GetAccessTokenAsync();
                if (string.IsNullOrEmpty(accessToken))
                {
                    Console.WriteLine("FCM: No se pudo obtener el Access Token.");
                    return false;
                }

                // El endpoint de la v1 usa el ID del proyecto de Firebase
                string fcmEndpoint = $"https://fcm.googleapis.com/v1/projects/{Secretos.FirebaseProjectId}/messages:send";

                // Armamos el JSON según el esquema que pide Google para notificaciones y datos
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
                                // Configuración específica para que Android haga ruido y vibre por defecto
                                sound = "default",
                                click_action = "FCM_PLUGIN_ACTIVITY"
                            }
                        },
                        // Los datos extra sirven para que la app tome decisiones (ej. abrir un pedido específico)
                        data = datosExtra ?? new Dictionary<string, string>()
                    }
                };

                // Serializamos el objeto a string para el cuerpo de la petición
                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // Preparamos la petición POST con la cabecera de autorización Bearer
                var request = new HttpRequestMessage(HttpMethod.Post, fcmEndpoint);
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {accessToken}");
                request.Content = content;

                // Lanzamos el envío y esperamos la respuesta del servidor de Google
                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Éxito: el mensaje está en manos de Google para entregarlo
                    Console.WriteLine($"FCM: Notificación enviada exitosamente al token: ...{token[^6..]}");
                    return true;
                }
                else
                {
                    // Si falló (ej. token inválido), imprimimos el error para debugguear
                    Console.WriteLine($"FCM Error: {response.StatusCode} — {responseBody}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                // Cualquier error de red o de código cae aquí
                Console.WriteLine($"FCM Exception: {ex.Message}");
                return false;
            }
        }

        // Envía un saludo personalizado al usuario cuando entra a la app
        public static async Task NotificarBienvenidaAsync(DataBaseServices db, string userId, string nombre, bool esAdmin)
        {
            try
            {
                // Recuperamos el token desde Supabase para saber a qué cel mandarlo
                var token = await db.ObtenerTokenPorClienteIdAsync(userId);
                if (string.IsNullOrEmpty(token)) return;

                // Diferenciamos el saludo si es un cliente o si es el dueño/admin de la pizzería
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

        // Le avisa al cliente que su orden ya está en el sistema
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

        // Alerta a los administradores si un cliente arrepentido canceló su pedido
        public static async Task NotificarCancelacionClienteAAdminsAsync(DataBaseServices db, string pedidoIdVisible, string nombreCliente)
        {
            try
            {
                // Obtenemos todos los tokens de los celulares de los administradores
                var tokensAdmins = await db.ObtenerTokenAdminAsync();
                if (tokensAdmins == null || tokensAdmins.Count == 0) return;

                var datos = new Dictionary<string, string>
                {
                    { "tipo", "cancelacion_cliente" },
                    { "pedido_id", pedidoIdVisible },
                    { "timestamp", DateTime.UtcNow.ToString("o") }
                };

                // Mandamos la alerta a cada uno de ellos
                foreach (var token in tokensAdmins)
                {
                    // No esperamos (await) cada envío para no retrasar el hilo principal si son muchos tokens
                    _ = EnviarNotificacionAsync(token, $"❌ Pedido {pedidoIdVisible} Cancelado", $"El cliente {nombreCliente} ha cancelado su pedido.", datos);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FCM Error: {ex.Message}");
            }
        }

        // Alerta crítica para cocina: ¡Alguien quiere pizza!
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

        // Esta es la que más usa el Admin para avisar al cliente cómo va su pizza
        public static async Task NotificarCambioEstadoAClienteAsync(DataBaseServices db, string clienteId, string nuevoEstado, string pedidoIdVisible)
        {
            try
            {
                var tokenCliente = await db.ObtenerTokenPorClienteIdAsync(clienteId);
                if (string.IsNullOrEmpty(tokenCliente)) return;

                // Elegimos un emoji según el estado para que la notificación se vea más premium
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
ine($"FCM Error: {ex.Message}");
            }
        }
    }
}