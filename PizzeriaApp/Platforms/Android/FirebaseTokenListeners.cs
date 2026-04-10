using Android.Gms.Tasks;
using Java.Lang;

namespace PizzeriaApp.Platforms.Android
{
    // Este listener se dispara cuando Firebase nos entrega exitosamente el token del dispositivo
    // Es vital para que luego podamos enviarle notificaciones de "¡Tu pizza está en camino!" al usuario
    public class TokenSuccessListener : Java.Lang.Object, IOnSuccessListener
    {
        // Usamos esto para avisarle al resto de la app C# que ya tenemos el resultado
        private readonly TaskCompletionSource<string?> _tcs;

        public TokenSuccessListener(TaskCompletionSource<string?> tcs)
        {
            // Inyectamos la fuente de la tarea para poder marcarla como terminada
            _tcs = tcs;
        }

        // Aquí es donde Android nos avisa que todo salió bien
        public void OnSuccess(Java.Lang.Object result)
        {
            // Convertimos el objeto de Java a string y cerramos la tarea con el token
            _tcs.TrySetResult(result?.ToString());
        }
    }

    // Por el otro lado, este se encarga de cachar cualquier error al intentar hablar con los servicios de Google
    public class TokenFailureListener : Java.Lang.Object, IOnFailureListener
    {
        // También necesitamos la TaskCompletionSource para reportar el fallo
        private readonly TaskCompletionSource<string?> _tcs;

        public TokenFailureListener(TaskCompletionSource<string?> tcs)
        {
            // Guardamos la referencia para disparar la excepción si algo truena
            _tcs = tcs;
        }

        // Este método truena si, por ejemplo, los Google Play Services no están configurados o no hay internet
        public void OnFailure(Java.Lang.Exception e)
        {
            // Si falló, lanzamos una excepción de .NET con el mensaje que nos dio Android para saber qué pasó
            _tcs.TrySetException(new System.Exception($"FCM token retrieval failed: {e.Message}"));
        }
    }
}
