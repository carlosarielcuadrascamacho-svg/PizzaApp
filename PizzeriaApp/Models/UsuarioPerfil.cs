using System;
using System.Collections.Generic;
using System.Text;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace PizzeriaApp.Models
{
    [Table("perfiles")]
    public class UsuarioPerfil: BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("es_admin")]
        public bool EsAdmin { get; set; }

        public string Nombre { get; set; }
    }
}
