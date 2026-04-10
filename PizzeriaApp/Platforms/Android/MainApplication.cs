using System;
using Android.App;
using Android.Runtime;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;

namespace PizzeriaApp
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership) : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override void OnCreate()
        {
            base.OnCreate();

            // Firebase se inicializa automáticamente a través de google-services.json
            // y el servicio PizzeriaFirebaseMessagingService registrado en el manifiesto.
            // No se necesita inicialización manual adicional.
            //
            // El token FCM será capturado por PizzeriaFirebaseMessagingService.OnNewToken()
            // y guardado en Supabase desde Login.xaml.cs después del login exitoso.
        }
    }
}