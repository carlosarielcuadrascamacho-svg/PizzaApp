using System;
using System.Threading.Tasks;
using Supabase;

namespace PizzeriaApp.Services
{
    public static class SupabaseClientFactory
    {
        private static Supabase.Client? _client;
        private static bool _initialized = false;

        public static Supabase.Client? Client => _client;

        public static bool IsInitialized => _initialized;

        public static Supabase.Client GetClientOrThrow()
        {
            if (_client == null)
                throw new InvalidOperationException("Supabase client not initialized. Call InitializeAsync first.");
            return _client;
        }

        public static async Task InitializeAsync(string url, string key)
        {
            if (_initialized)
                return;

            var options = new SupabaseOptions { AutoConnectRealtime = true };
            _client = new Supabase.Client(url, key, options);

            await _client.InitializeAsync();
            _initialized = true;
        }
    }
}