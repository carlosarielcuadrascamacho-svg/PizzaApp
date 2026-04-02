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

            // Evitar usar Activity u obtener el looper desde una Activity aquí.
            // Si se necesita el looper principal, usar Android.OS.Looper.MainLooper.
            // Inicializaciones que solo requieren el contexto de la aplicación deben ir aquí.
            // Inicializaciones que requieren una Activity válida deben diferirse a MainActivity.OnCreate.

            // Ejemplo seguro de obtener el looper principal:
            // var mainLooper = Android.OS.Looper.MainLooper;
        }
    }
}
