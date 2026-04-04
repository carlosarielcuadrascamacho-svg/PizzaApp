using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace PizzeriaApp.Models
{
    [Table("pedidos")]
    public class Pedido : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("cliente_id")]
        public string ClienteId { get; set; }

        [Column("fecha")]
        public DateTime Fecha { get; set; }

        [Column("estado")]
        public string Estado { get; set; }

        [Column("total")]
        public decimal Total { get; set; }
    }
}