using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PizzeriaApp.Models
{
    public class ItemCarrito : INotifyPropertyChanged
    {
        public Producto Producto { get; set; }

        private int _cantidad;
        public int Cantidad 
        { 
            get => _cantidad; 
            set 
            {
                if (_cantidad != value)
                {
                    _cantidad = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Subtotal));
                }
            }
        }

        public string Comentarios { get; set; }

        public decimal Subtotal => Producto != null ? Producto.Precio * Cantidad : 0;

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}