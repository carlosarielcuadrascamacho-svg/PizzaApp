using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using Newtonsoft.Json; // Obligatorio para [JsonIgnore]

namespace PizzeriaApp.Models
{
    [Table("perfiles")] // Pon el nombre exacto de tu tabla aquí
    public class UsuarioPerfil : BaseModel
    {
        [PrimaryKey("id", false)] // false = la BD genera el ID
        public string Id { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("es_admin")] // El nombre exacto de tu columna booleana
        public bool EsAdmin { get; set; }

        // Propiedad en memoria: Se usa en la UI, pero Supabase la ignora por completo
        [JsonIgnore]
        public string Nombre { get; set; }
    }
}