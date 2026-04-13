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
        private readonly ServicioCatalogo _servicioCatalogo;
        private readonly ServicioPedidos _servicioPedidos;

        public OrderController(ServicioCatalogo servicioCatalogo, ServicioPedidos servicioPedidos)
        {
            _servicioCatalogo = servicioCatalogo;
            _servicioPedidos = servicioPedidos;
        }

        // Obtiene el catálogo de productos activos
        public async Task<List<Producto>> ObtenerMenuAsync()
        {
            return await _servicioCatalogo.ObtenerProductosActivosAsync();
        }

        // Procesa la creación de un nuevo pedido
        public async Task<bool> ProcesarPedidoAsync(UsuarioPerfil cliente, ObservableCollection<ItemCarrito> carrito, string mesa, string comentario)
        {
            if (carrito == null || carrito.Count == 0) return false;

            decimal total = carrito.Sum(i => i.Subtotal);
            string mesaFinal = string.IsNullOrEmpty(mesa) ? "Mostrador" : mesa;

            bool exito = await _servicioPedidos.CrearPedidoAsync(cliente.Id, carrito.ToList(), total, mesaFinal, comentario);

            if (exito)
            {
                // Disparamos las notificaciones (estratégicamente desde el controlador)
                _ = NotificationService.NotificarNuevoPedidoAAdminsAsync();
                _ = NotificationService.NotificarPedidoRecibidoAClienteAsync(cliente.Id);
            }

            return exito;
        }

        // Obtiene el historial específico de un cliente
        public async Task<List<Pedido>> ObtenerHistorialClienteAsync(string clienteId)
        {
            return await _servicioPedidos.ObtenerHistorialClienteAsync(clienteId);
        }

        // Cancela una orden desde la perspectiva del cliente o admin
        public async Task<bool> CancelarOrdenAsync(Pedido pedido)
        {
            bool ok = await _servicioPedidos.ActualizarEstadoPedidoAsync(pedido.Id, "Cancelado");
            if (ok)
            {
                _ = NotificationService.NotificarCambioEstadoAClienteAsync(pedido.ClienteId, "Cancelado", pedido.IdVisible);
            }
            return ok;
        }
    }
}
