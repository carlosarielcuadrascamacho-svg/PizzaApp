using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PizzeriaApp.Models;
using PizzeriaApp.Services;

namespace PizzeriaApp.Controllers
{
    // El Controlador Administrativo maneja el dashboard, la cocina y los productos
    public class AdminController
    {
        private readonly DataBaseServices _db;

        public AdminController(DataBaseServices db)
        {
            _db = db;
        }

        // Obtiene las métricas financieras e historial del día para el dashboard
        public async Task<(decimal Ingresos, int Unidades, List<Pedido> Historial)> ObtenerReportesDiaAsync()
        {
            return await _db.ObtenerMetricasDelDiaAsync();
        }

        // Obtiene los pedidos que están pendientes en cocina
        public async Task<List<Pedido>> ObtenerColaCocinaAsync()
        {
            return await _db.ObtenerPedidosActivosAsync();
        }

        // Cambia el estado de una orden (Ej: De En Preparación a Listo)
        public async Task<bool> ActualizarEstadoOrdenAsync(string pedidoId, string clienteId, string idVisible, string nuevoEstado)
        {
            bool ok = await _db.ActualizarEstadoPedidoAsync(pedidoId, nuevoEstado);
            if (ok)
            {
                // Notificamos al cliente sobre su pizza
                _ = NotificationService.NotificarCambioEstadoAClienteAsync(_db, clienteId, nuevoEstado, idVisible);
            }
            return ok;
        }

        // --- Gestión de Catálogo ---
        public async Task<List<Producto>> ObtenerTodoElCatalogoAsync()
        {
            return await _db.ObtenerCatalogoCompletoAsync();
        }

        public async Task<bool> GuardarProductoAsync(Producto p, bool esNuevo)
        {
            if (esNuevo)
                return await _db.InsertarProductoAsync(p);
            else
                return await _db.ActualizarProductoAsync(p);
        }

        // Métodos específicos requeridos por las vistas
        public async Task<bool> GuardarNuevoProductoAsync(Producto p)
        {
            return await _db.InsertarProductoAsync(p);
        }

        public async Task<bool> ActualizarProductoAsync(Producto p)
        {
            return await _db.ActualizarProductoAsync(p);
        }

        public async Task<UsuarioPerfil?> ObtenerDetalleClienteAsync(string clienteId)
        {
            return await _db.ObtenerPerfilAsync(clienteId);
        }

        public async Task<bool> CambiarDisponibilidadProductoAsync(string id, bool activo)
        {
            return await _db.CambiarEstadoProductoAsync(id, activo);
        }
    }
}
