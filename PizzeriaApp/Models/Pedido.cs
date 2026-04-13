using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace PizzeriaApp.Models
{
    [Table("pedidos")]
    public class Pedido : BaseModel, INotifyPropertyChanged
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("cliente_id")]
        public string ClienteId { get; set; }

        private DateTime _fecha;
        [Column("fecha")]
        public DateTime Fecha 
        { 
            get => _fecha; 
            set => _fecha = value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Local) : value; 
        }

        [JsonIgnore]
        public DateTime FechaLocal => Fecha.ToLocalTime();

        private string _estado;
        [Column("estado")]
        public string Estado 
        { 
            get => _estado; 
            set 
            { 
                if (_estado != value)
                {
                    _estado = value; 
                    OnPropertyChanged(); 
                    // Notificar cambios en propiedades dependientes
                    OnPropertyChanged(nameof(IsNotCancelled));
                    OnPropertyChanged(nameof(EsUrgente));
                    OnPropertyChanged(nameof(ColorEstado));
                    OnPropertyChanged(nameof(IsOrdenado));
                    OnPropertyChanged(nameof(IsEnPreparacion));
                    OnPropertyChanged(nameof(IsListo));
                    OnPropertyChanged(nameof(SiguienteEstado));
                    OnPropertyChanged(nameof(TextoBotonSiguiente));
                    OnPropertyChanged(nameof(ProgressIndex));
                    OnPropertyChanged(nameof(ProgressPercent));
                }
            } 
        }

        [Column("total")]
        public decimal Total { get; set; }

        [Column("mesa")]
        public string Mesa { get; set; }

        [Column("comentario")]
        public string Comentario { get; set; }

        // Propiedades de UI (No persistidas en DB directamente)
        [JsonIgnore]
        public string DetalleResumen { get; set; }

        [JsonIgnore]
        public ObservableCollection<DetallePedido> Detalles { get; set; } = new ObservableCollection<DetallePedido>();

        [JsonIgnore]
        public bool IsNotCancelled => Estado != "Cancelado" && Estado != "Entregado";

        [JsonIgnore]
        public bool TieneComentario => !string.IsNullOrWhiteSpace(Comentario);

        [JsonIgnore]
        public string IdVisible => !string.IsNullOrEmpty(Id) && Id.Length >= 6 ? $"#{Id.Substring(0, 6).ToUpper()}" : "#N/A";

        [JsonIgnore]
        public UsuarioPerfil Cliente { get; set; }

        [JsonIgnore]
        public bool TieneInformacionCliente => Cliente != null && !string.IsNullOrEmpty(Cliente.Nombre);

        [JsonIgnore]
        public string MesaVisible => !string.IsNullOrEmpty(Mesa) ? $"📍 {Mesa}" : "🛵 Delivery";

        [JsonIgnore]
        public bool EsUrgente => (DateTime.UtcNow - Fecha.ToUniversalTime()).TotalMinutes > 15 && Estado == "Ordenado";

        private bool _isExpanded;
        [JsonIgnore]
        public bool IsExpanded 
        { 
            get => _isExpanded; 
            set { _isExpanded = value; OnPropertyChanged(); } 
        }

        [JsonIgnore]
        public string TiempoRelativo
        {
            get
            {
                // Calculamos la diferencia siempre en UTC
                var diff = DateTime.UtcNow - Fecha.ToUniversalTime();
                
                if (diff.TotalMinutes < 1) return "Hace un momento";
                if (diff.TotalMinutes < 60) return $"Hace {(int)diff.TotalMinutes} min";
                if (diff.TotalHours < 24) return $"Hace {(int)diff.TotalHours}h {(int)(diff.TotalMinutes % 60)}min";
                
                // Para pedidos de más de un día, mostramos la fecha en hora local del dispositivo
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
                    "Ordenado" => "#607D8B",        // Gris Azulado (Pendiente)
                    "En preparación" => "#FF9800",  // Naranja (Cocinando)
                    "Listo" => "#00C853",           // Verde (Para entregar)
                    "Entregado" => "#4CAF50",       // Verde Olivo (Finalizado)
                    "Cancelado" => "#F44336",       // Rojo
                    _ => "#FF4B3A"
                };
            }
        }

        // Propiedades para saber qué botón mostrar en la interfaz
        [JsonIgnore]
        public bool IsOrdenado => Estado == "Ordenado";
        
        [JsonIgnore]
        public bool IsEnPreparacion => Estado == "En preparación";
        
        [JsonIgnore]
        public bool IsListo => Estado == "Listo";

        [JsonIgnore]
        public string SiguienteEstado => Estado switch
        {
            "Ordenado" => "En preparación",
            "En preparación" => "Listo",
            "Listo" => "Entregado",
            _ => "Entregado"
        };

        [JsonIgnore]
        public string TextoBotonSiguiente => Estado switch
        {
            "Ordenado" => "🔥 EMPEZAR",
            "En preparación" => "✅ TERMINAR",
            "Listo" => "🚚 ENTREGAR",
            _ => "✓ OK"
        };

        // Lógica de progreso para el Stepper visual (0-3)
        [JsonIgnore]
        public int ProgressIndex => Estado switch
        {
            "Ordenado" => 1,
            "En preparación" => 2,
            "Listo" => 3,
            "Entregado" => 4,
            _ => 1
        };

        [JsonIgnore]
        public double ProgressPercent => ProgressIndex / 4.0;

        public void NotifyUrgencyChanged()
        {
            OnPropertyChanged(nameof(EsUrgente));
            OnPropertyChanged(nameof(TiempoRelativo));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}