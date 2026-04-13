using System;
using System.Threading.Tasks;
using PizzeriaApp.Config;
using Supabase;

namespace PizzeriaApp.Services
{
    /// <summary>
    /// Clase base para todos los servicios que interactúan con Supabase.
    /// Centraliza la inicialización del cliente y el acceso seguro al mismo.
    /// </summary>
    public abstract class ServicioBase
    {
        // Almacenamos el cliente de Supabase de forma estática para que todas las instancias compartan la misma conexión
        protected static Supabase.Client? _supabase;

        // Propiedad que devuelve el cliente o lanza una excepción clara si no ha sido inicializado
        protected Supabase.Client Client => _supabase ?? SupabaseClientFactory.GetClientOrThrow();

        // Bandera para evitar inicializaciones redundantes
        private static bool _estaInicializado = false;

        public ServicioBase()
        {
            // Intentamos obtener el cliente del Factory al instanciar
            if (_supabase == null)
            {
                _supabase = SupabaseClientFactory.Client;
            }
        }

        /// <summary>
        /// Asegura que la conexión con el backend esté establecida.
        /// </summary>
        public async Task InicializarAsync()
        {
            if (_estaInicializado) return;

            try
            {
                // Usamos las credenciales configuradas en Secretos.cs
                await SupabaseClientFactory.InitializeAsync(Secretos.SupabaseUrl, Secretos.SupabaseApiKey);
                _supabase = SupabaseClientFactory.Client;
                _estaInicializado = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServicioBase] Error crítico de inicialización: {ex.Message}");
                throw;
            }
        }
    }
}
