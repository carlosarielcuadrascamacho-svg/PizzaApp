namespace PizzeriaApp.Models
{
    public class ItemCarrito
    {
        public Producto Producto { get; set; }

        public int Cantidad { get; set; }

        public decimal Subtotal => Producto.Precio * Cantidad;
    }
}