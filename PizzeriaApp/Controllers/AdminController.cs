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
        private readonly ServicioReportes _servicioReportes;
        private readonly ServicioPedidos _servicioPedidos;
        private readonly ServicioCatalogo _servicioCatalogo;
        private readonly ServicioPerfiles _servicioPerfiles;

        public AdminController(ServicioReportes servicioReportes, ServicioPedidos servicioPedidos, ServicioCatalogo servicioCatalogo)
        {
            _servicioReportes = servicioReportes;
            _servicioPedidos = servicioPedidos;
            _servicioCatalogo = servicioCatalogo;
            _servicioPerfiles = new ServicioPerfiles(); // Instanciamos localmente ya que es ligero
        }

        // Obtiene las métricas financieras e historial del día para el dashboard
        public async Task<(decimal Ingresos, int Unidades, List<Pedido> Historial)> ObtenerReportesDiaAsync()
        {
            return await _servicioReportes.ObtenerMetricasDelDiaAsync();
        }

        // Obtiene los pedidos que están pendientes en cocina
        public async Task<List<Pedido>> ObtenerColaCocinaAsync()
        {
            return await _servicioPedidos.ObtenerPedidosActivosAsync();
        }

        // Cambia el estado de una orden (Ej: De En Preparación a Listo)
        public async Task<bool> ActualizarEstadoOrdenAsync(string pedidoId, string clienteId, string idVisible, string nuevoEstado)
        {
            bool ok = await _servicioPedidos.ActualizarEstadoPedidoAsync(pedidoId, nuevoEstado);
            if (ok)
            {
                // Notificamos al cliente sobre su pizza
                _ = NotificationService.NotificarCambioEstadoAClienteAsync(clienteId, nuevoEstado, idVisible);
            }
            return ok;
        }

        // --- Gestión de Catálogo ---
        public async Task<List<Producto>> ObtenerTodoElCatalogoAsync()
        {
            return await _servicioCatalogo.ObtenerCatalogoCompletoAsync();
        }

        public async Task<bool> GuardarNuevoProductoAsync(Producto p)
        {
            return await _servicioCatalogo.InsertarProductoAsync(p);
        }

        public async Task<bool> ActualizarProductoAsync(Producto p)
        {
            return await _servicioCatalogo.ActualizarProductoAsync(p);
        }

        public async Task<bool> CambiarDisponibilidadProductoAsync(string id, bool activo)
        {
            return await _servicioCatalogo.CambiarDisponibilidadProductoAsync(id, activo);
        }

        // Obtiene el perfil del cliente para el detalle de la orden
        public async Task<UsuarioPerfil?> ObtenerDetalleClienteAsync(string clienteId)
        {
            return await _servicioPerfiles.ObtenerPerfilAsync(clienteId);
        }
    }
}
