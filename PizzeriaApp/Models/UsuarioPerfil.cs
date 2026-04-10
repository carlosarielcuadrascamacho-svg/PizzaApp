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

        [Column("nombre")]
        public string Nombre { get; set; }

        [Column("direccion")]
        public string Direccion { get; set; }

        [Column("telefono")]
        public string Telefono { get; set; }

        [Column("foto_perfil")]
        public string FotoPerfil { get; set; }

        [Column("fcm_token")]
        public string FcmToken { get; set; }
    }
}