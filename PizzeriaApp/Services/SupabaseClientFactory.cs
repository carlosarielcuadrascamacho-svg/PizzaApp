using System;
using System.Threading.Tasks;
using Supabase;

namespace PizzeriaApp.Services
{
    // Esta factory centraliza la conexión con Supabase para que no andemos creando clientes por todos lados
    public static class SupabaseClientFactory
    {
        // Guardamos la instancia del cliente y un flag para saber si ya estamos listos para hacer queries
        private static Supabase.Client? _client;
        private static bool _initialized = false;

        // Propiedad pública para acceder al cliente desde los servicios de la pizzería
        public static Supabase.Client? Client => _client;

        // Útil para que la UI sepa si ya puede intentar cargar el menú o el perfil
        public static bool IsInitialized => _initialized;

        // Método de seguridad: si intentamos usar el cliente antes de tiempo, lanzamos un error claro
        public static Supabase.Client GetClientOrThrow()
        {
            if (_client == null)
                throw new InvalidOperationException("Supabase client not initialized. Call InitializeAsync first.");
            return _client;
        }

        // Este es el punto de arranque; lo llamamos al iniciar la app con la URL y la Key de nuestro proyecto
        public static async Task InitializeAsync(string url, string key)
        {
            // Si ya está iniciado, no perdemos tiempo volviendo a conectarnos
            if (_initialized)
                return;

            // Configuramos Supabase; habilitamos Realtime para que los estados de los pedidos se actualicen en vivo
            var options = new SupabaseOptions { AutoConnectRealtime = true };
            _client = new Supabase.Client(url, key, options);

            // Ejecutamos la inicialización asíncrona oficial de la librería
            await _client.InitializeAsync();
            _initialized = true;
        }
    }
}