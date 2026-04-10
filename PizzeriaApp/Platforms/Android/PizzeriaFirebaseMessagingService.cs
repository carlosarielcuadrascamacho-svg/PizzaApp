using Android.App;
using Firebase.Messaging;

namespace PizzeriaApp.Platforms.Android
{
    // Este servicio es el corazón de las notificaciones en Android. Se encarga de escuchar a Firebase
    // para saber cuándo hay nuevas promociones o si el pedido del cliente ya salió de la cocina.
    [Service(Exported = true)]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class PizzeriaFirebaseMessagingService : FirebaseMessagingService
    {
        // Evento para avisar al resto del sistema que tenemos un token fresco
        public static event Action<string>? TokenRefreshed;

        // Guardamos el último token por si alguna otra parte de la app lo necesita rápido
        public static string? LastKnownToken { get; private set; }

        // Este método se dispara cuando instalas la app o cuando Firebase decide que es hora de cambiar el token
        public override void OnNewToken(string token)
        {
            base.OnNewToken(token);
            // Actualizamos nuestra referencia local
            LastKnownToken = token;
            // Un log para debugguear fácil; solo mostramos el final del token por seguridad
            System.Diagnostics.Debug.WriteLine($"FCM: Nuevo token recibido: ...{token[^6..]}");

            // Le avisamos a quien esté escuchando (probablemente para guardarlo en la base de datos de Supabase)
            TokenRefreshed?.Invoke(token);
        }

        // Aquí es donde caen los mensajes cuando el usuario tiene la app abierta
        public override void OnMessageReceived(RemoteMessage message)
        {
            base.OnMessageReceived(message);

            // Extraemos la parte de la notificación del mensaje de Firebase
            var notification = message.GetNotification();
            if (notification != null)
            {
                System.Diagnostics.Debug.WriteLine($"FCM: Mensaje recibido — {notification.Title}: {notification.Body}");

                // Si el usuario está viendo la app, Android no muestra el globito arriba por sí solo,
                // así que nosotros forzamos una notificación local para que no pase desapercibida.
                MostrarNotificacionLocal(notification.Title, notification.Body);
            }
        }

        // Método para pintar la notificación manualmente cuando la app está en primer plano
        private void MostrarNotificacionLocal(string? titulo, string? cuerpo)
        {
            // ID del canal, obligatorio para Android Oreo en adelante
            var channelId = "pizzeria_notifications";

            // Si el sistema es moderno (Android 8+), tenemos que crear el canal de comunicación
            if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(channelId, "Notificaciones de Pizzería",
                    NotificationImportance.High)
                {
                    Description = "Notificaciones de pedidos y estados"
                };

                // Registramos el canal en el sistema
                var notificationManager = (NotificationManager?)GetSystemService(NotificationService);
                notificationManager?.CreateNotificationChannel(channel);
            }

            // Construimos la notificación visual
            var builder = new Notification.Builder(this, channelId)
                .SetContentTitle(titulo ?? "PizzeriaApp")
                .SetContentText(cuerpo ?? "Tienes una nueva notificación")
                .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo) // Icono por defecto del sistema
                .SetAutoCancel(true); // Se quita solita cuando el usuario le pica

            // Lanzamos la notificación con un ID aleatorio para que no se encimen si llegan varias seguidas
            var notifManager = (NotificationManager?)GetSystemService(NotificationService);
            notifManager?.Notify(new Random().Next(1000, 9999), builder.Build());
        }
    }
}
