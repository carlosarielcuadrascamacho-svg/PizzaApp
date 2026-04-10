using Android.App;
using Firebase.Messaging;

namespace PizzeriaApp.Platforms.Android
{
    /// <summary>
    /// Servicio nativo de Firebase Messaging para Android.
    /// Captura el token FCM y recibe mensajes push (incluso con la app en background).
    /// </summary>
    [Service(Exported = true)]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class PizzeriaFirebaseMessagingService : FirebaseMessagingService
    {
        /// <summary>
        /// Evento estático para notificar a la capa compartida cuando un nuevo token es generado.
        /// </summary>
        public static event Action<string>? TokenRefreshed;

        /// <summary>
        /// Último token conocido (accesible desde cualquier parte de la app).
        /// </summary>
        public static string? LastKnownToken { get; private set; }

        /// <summary>
        /// Llamado por Firebase cuando se genera o renueva el token FCM del dispositivo.
        /// Esto sucede al instalar la app, cuando Firebase renueva el token, o cuando
        /// se elimina el Instance ID.
        /// </summary>
        public override void OnNewToken(string token)
        {
            base.OnNewToken(token);
            LastKnownToken = token;
            System.Diagnostics.Debug.WriteLine($"FCM: Nuevo token recibido: ...{token[^6..]}");

            // Notificar a la capa compartida
            TokenRefreshed?.Invoke(token);
        }

        /// <summary>
        /// Llamado cuando se recibe un mensaje push (tanto con la app en foreground como data messages).
        /// Los mensajes tipo "notification" son manejados automáticamente por el sistema Android
        /// cuando la app está en background.
        /// </summary>
        public override void OnMessageReceived(RemoteMessage message)
        {
            base.OnMessageReceived(message);

            var notification = message.GetNotification();
            if (notification != null)
            {
                System.Diagnostics.Debug.WriteLine($"FCM: Mensaje recibido — {notification.Title}: {notification.Body}");

                // Si la app está en foreground, mostrar notificación local
                MostrarNotificacionLocal(notification.Title, notification.Body);
            }
        }

        /// <summary>
        /// Muestra una notificación local cuando la app está en primer plano.
        /// Android no muestra automáticamente las notificaciones push si la app está abierta.
        /// </summary>
        private void MostrarNotificacionLocal(string? titulo, string? cuerpo)
        {
            var channelId = "pizzeria_notifications";

            // Crear canal de notificación (requerido en Android 8+)
            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(channelId, "Notificaciones de Pizzería",
                    NotificationImportance.High)
                {
                    Description = "Notificaciones de pedidos y estados"
                };

                var notificationManager = (NotificationManager?)GetSystemService(NotificationService);
                notificationManager?.CreateNotificationChannel(channel);
            }

            var builder = new Notification.Builder(this, channelId)
                .SetContentTitle(titulo ?? "PizzeriaApp")
                .SetContentText(cuerpo ?? "Tienes una nueva notificación")
                .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)
                .SetAutoCancel(true);

            var notifManager = (NotificationManager?)GetSystemService(NotificationService);
            notifManager?.Notify(new Random().Next(1000, 9999), builder.Build());
        }
    }
}
