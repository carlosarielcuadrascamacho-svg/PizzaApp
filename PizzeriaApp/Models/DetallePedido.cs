using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using Newtonsoft.Json;

namespace PizzeriaApp.Models
{
    [Table("detalle_pedidos")]
    public class DetallePedido : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("pedido_id")]
        public string PedidoId { get; set; }

        [Column("producto_id")]
        public string ProductoId { get; set; }

        [Column("cantidad")]
        public int Cantidad { get; set; }

        [Column("precio_unitario")]
        public decimal PrecioUnitario { get; set; }

        [JsonIgnore]
        public decimal Subtotal => Cantidad * PrecioUnitario;
    }
}