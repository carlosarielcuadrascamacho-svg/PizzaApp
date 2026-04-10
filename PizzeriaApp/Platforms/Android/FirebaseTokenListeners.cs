using Android.Gms.Tasks;
using Java.Lang;

namespace PizzeriaApp.Platforms.Android
{
    /// <summary>
    /// Listener de éxito para la obtención del token FCM desde Firebase.
    /// Convierte la callback de Java en una Task de .NET mediante TaskCompletionSource.
    /// </summary>
    public class TokenSuccessListener : Java.Lang.Object, IOnSuccessListener
    {
        private readonly TaskCompletionSource<string?> _tcs;

        public TokenSuccessListener(TaskCompletionSource<string?> tcs)
        {
            _tcs = tcs;
        }

        public void OnSuccess(Java.Lang.Object result)
        {
            _tcs.TrySetResult(result?.ToString());
        }
    }

    /// <summary>
    /// Listener de fallo para la obtención del token FCM desde Firebase.
    /// </summary>
    public class TokenFailureListener : Java.Lang.Object, IOnFailureListener
    {
        private readonly TaskCompletionSource<string?> _tcs;

        public TokenFailureListener(TaskCompletionSource<string?> tcs)
        {
            _tcs = tcs;
        }

        public void OnFailure(Java.Lang.Exception e)
        {
            _tcs.TrySetException(new System.Exception($"FCM token retrieval failed: {e.Message}"));
        }
    }
}
