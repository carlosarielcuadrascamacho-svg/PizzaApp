using System.Net.Http.Headers;
using PizzeriaApp.Config;

namespace PizzeriaApp.Services
{
    public class SupabaseClientFactory
    {
        public HttpClient CreateClient()
        {
            var cliente = new HttpClient();

            // 1. Establecemos la URL base a la API de PostgreSQL
            cliente.BaseAddress = new Uri(Secretos.SupabaseUrl);

            // 2. Inyectamos los Headers obligatorios de Supabase
            cliente.DefaultRequestHeaders.Add("apikey", Secretos.SupabaseApiKey);
            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Secretos.SupabaseApiKey);

            // 3. Forzamos a que toda la comunicación de ida y vuelta sea en formato JSON
            cliente.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return cliente;
        }
    }
}