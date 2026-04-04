using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace PizzeriaApp.Models
{
    [Table("productos")]
    public class Producto : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; }

        [Column("descripcion")]
        public string Descripcion { get; set; }

        [Column("precio")]
        public decimal Precio { get; set; }

        [Column("imagen_url")]
        public string ImagenUrl { get; set; }

        [Column("activo")]
        public bool Activo { get; set; }
    }
}