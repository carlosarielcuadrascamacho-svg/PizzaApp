using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.ObjectModel;
using Newtonsoft.Json;

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

        [Column("mesa")]
        public string Mesa { get; set; }

        // Propiedades de UI (No persistidas en DB directamente)
        [JsonIgnore]
        public string DetalleResumen { get; set; }

        [JsonIgnore]
        public ObservableCollection<DetallePedido> Detalles { get; set; } = new ObservableCollection<DetallePedido>();

        [JsonIgnore]
        public bool IsNotCancelled => Estado != "Cancelado" && Estado != "Entregado";

        [JsonIgnore]
        public string IdVisible => !string.IsNullOrEmpty(Id) && Id.Length >= 6 ? $"#{Id.Substring(0, 6).ToUpper()}" : "#N/A";

        [JsonIgnore]
        public string MesaVisible => !string.IsNullOrEmpty(Mesa) ? $"Mesa: {Mesa}" : "Delivery / Sin mesa";

        [JsonIgnore]
        public string TiempoRelativo
        {
            get
            {
                var diff = DateTime.UtcNow - Fecha;
                if (diff.TotalMinutes < 1) return "Hace un momento";
                if (diff.TotalMinutes < 60) return $"Hace {(int)diff.TotalMinutes} min";
                if (diff.TotalHours < 24) return $"Hace {(int)diff.TotalHours}h {(int)(diff.TotalMinutes % 60)}min";
                return Fecha.ToLocalTime().ToString("dd/MM HH:mm");
            }
        }

        [JsonIgnore]
        public string ColorEstado
        {
            get
            {
                return Estado switch
                {
                    "En preparación" => "#FF9800",
                    "Listo" => "#00C853",
                    "Entregado" => "#4CAF50",
                    "Cancelado" => "#F44336",
                    _ => "#FF4B3A"
                };
            }
        }
    }
}