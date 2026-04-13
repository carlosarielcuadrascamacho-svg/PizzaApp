using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using PizzeriaApp.Models;
using PizzeriaApp.Services;

namespace PizzeriaApp.Controllers
{
    // El Controlador de Pedidos maneja todo el flujo de compra y consulta de historial
    public class OrderController
    {
        private readonly DataBaseServices _db;

        public OrderController(DataBaseServices db)
        {
            _db = db;
        }

        // Obtiene el catálogo de productos activos
        public async Task<List<Producto>> ObtenerMenuAsync()
        {
            return await _db.ObtenerProductosActivosAsync();
        }

        // Procesa la creación de un nuevo pedido
        public async Task<bool> ProcesarPedidoAsync(UsuarioPerfil cliente, ObservableCollection<ItemCarrito> carrito, string mesa, string comentario)
        {
            if (carrito == null || carrito.Count == 0) return false;

            decimal total = carrito.Sum(i => i.Subtotal);
            string estadoInicial = "Ordenado";
            string mesaFinal = string.IsNullOrEmpty(mesa) ? "Mostrador" : mesa;

            bool exito = await _db.CrearPedidoV2Async(cliente.Id, carrito.ToList(), total, estadoInicial, mesaFinal, comentario);

            if (exito)
            {
                // Disparamos las notificaciones (estratégicamente desde el controlador)
                _ = NotificationService.NotificarNuevoPedidoAAdminsAsync(_db);
                _ = NotificationService.NotificarPedidoRecibidoAClienteAsync(_db, cliente.Id);
            }

            return exito;
        }

        // Obtiene el historial específico de un cliente
        public async Task<List<Pedido>> ObtenerHistorialClienteAsync(string clienteId)
        {
            return await _db.ObtenerHistorialClienteAsync(clienteId);
        }

        // Cancela una orden desde la perspectiva del cliente o admin
        public async Task<bool> CancelarOrdenAsync(Pedido pedido)
        {
            bool ok = await _db.ActualizarEstadoPedidoAsync(pedido.Id, "Cancelado");
            if (ok)
            {
                _ = NotificationService.NotificarCambioEstadoAClienteAsync(_db, pedido.ClienteId, "Cancelado", pedido.IdVisible);
            }
            return ok;
        }
    }
}
