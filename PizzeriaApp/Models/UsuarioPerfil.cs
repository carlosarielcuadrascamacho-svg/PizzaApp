using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using Newtonsoft.Json; 

namespace PizzeriaApp.Models
{
    [Table("perfiles")] 
    public class UsuarioPerfil : BaseModel
    {
        [PrimaryKey("id", false)] 
        public string Id { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("es_admin")] 
        public bool EsAdmin { get; set; }

        [JsonIgnore]
        public string Nombre { get; set; }
    }
}