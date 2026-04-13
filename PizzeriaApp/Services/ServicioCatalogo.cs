using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PizzeriaApp.Models;

namespace PizzeriaApp.Services
{
    /// <summary>
    /// Gestiona todo lo relacionado con el menú y los productos de la pizzería.
    /// </summary>
    public class ServicioCatalogo : ServicioBase
    {
        // Traemos solo las pizzas y complementos que sí están disponibles para el público
        public async Task<List<Producto>> ObtenerProductosActivosAsync()
        {
            try
            {
                var respuesta = await Client.From<Producto>().Where(p => p.Activo == true).Get();
                return respuesta.Models;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServicioCatalogo] Error al obtener catálogo activo: {ex.Message}");
                return new List<Producto>();
            }
        }

        // Trae absolutamente todas las pizzas, activas o no (para el panel del administrador)
        public async Task<List<Producto>> ObtenerCatalogoCompletoAsync()
        {
            try
            {
                var respuesta = await Client.From<Producto>().Get();
                return respuesta.Models;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServicioCatalogo] Error obteniendo catálogo completo: {ex.Message}");
                return new List<Producto>();
            }
        }

        // Agregamos una nueva pizza al catálogo
        public async Task<bool> InsertarProductoAsync(Producto nuevoProducto)
        {
            try
            {
                var respuesta = await Client.From<Producto>().Insert(nuevoProducto);
                return respuesta.Models.Count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServicioCatalogo] Error al insertar producto: {ex.Message}");
                return false;
            }
        }

        // Editamos los datos de un producto existente
        public async Task<bool> ActualizarProductoAsync(Producto p)
        {
            try
            {
                var actualizacion = await Client.From<Producto>()
                    .Where(x => x.Id == p.Id)
                    .Set(x => x.Nombre, p.Nombre)
                    .Set(x => x.Descripcion, p.Descripcion)
                    .Set(x => x.Precio, p.Precio)
                    .Set(x => x.Categoria, p.Categoria)
                    .Set(x => x.ImagenBase64, p.ImagenBase64)
                    .Set(x => x.Activo, p.Activo)
                    .Update();

                return actualizacion.Models.Count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServicioCatalogo] Error al actualizar producto: {ex.Message}");
                return false;
            }
        }

        // Habilitamos o deshabilitamos un producto del menú rápidamente
        public async Task<bool> CambiarDisponibilidadProductoAsync(string id, bool activo)
        {
            try
            {
                var actualizacion = await Client.From<Producto>()
                    .Where(p => p.Id == id)
                    .Set(p => p.Activo, activo)
                    .Update();
                return actualizacion.Models.Count > 0;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"[ServicioCatalogo] Error cambiando disponibilidad: {ex.Message}");
                return false;
            }
        }
    }
}
