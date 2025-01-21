using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Prueba2Hotel.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class SerAdicionalesController : ControllerBase
    {

        private readonly AppDBContext _appDBContext;

        public SerAdicionalesController(AppDBContext appDBContext)
        {
            _appDBContext = appDBContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetServicios()
        {
            return Ok(await _appDBContext.ServiciosAdicionales.Select(r => new { r.Id, r.ReservaId, r.Descripcion, r.Costo}).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> PostServicio([FromBody] ServiciosAdicionales servicio)
        {
            if (!ModelState.IsValid)
            {
                // Errores validacion
                var errores = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return Ok(new { message = "Errores de validación", errores });
            }

            UtilsServiciosAdicionales utilsServiciosAdicionales = new UtilsServiciosAdicionales(_appDBContext);

            string mensaje = utilsServiciosAdicionales.ValidarServicio(servicio);

            if (mensaje != "")
            {
                return Ok(new { message = mensaje });
            }

            _appDBContext.ServiciosAdicionales.Add(servicio);
            await _appDBContext.SaveChangesAsync();
            return Ok(servicio);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutServicio(int id, [FromBody] ServiciosAdicionales servicio)
        {
            if (!ModelState.IsValid)
            {
                // Errores validacion
                var errores = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return Ok(new { message = "Errores de validación", errores });
            }

            servicio.Id = id;

            UtilsServiciosAdicionales utilsServiciosAdicionales = new UtilsServiciosAdicionales(_appDBContext);

            string mensaje = utilsServiciosAdicionales.ValidarServicio(servicio);

            if (mensaje != "")
            {
                return Ok(new { message = mensaje });
            }

            try
            {
                _appDBContext.Entry(servicio).State = EntityState.Modified;
                await _appDBContext.SaveChangesAsync();
                return Ok(new { message = servicio });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _appDBContext.ServiciosAdicionales.AnyAsync(s => s.Id == id))
                {
                    return NotFound(new { message = "Servicio adicional no encontrado." });
                }
                else
                {
                    return NotFound(new { message = "No se pudo actualizar el servicio." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteServicio(int id)
        {
            try
            {
                var servicio = await _appDBContext.ServiciosAdicionales.FindAsync(id);
                if (servicio == null)
                {
                    return Ok(new { message = "Servicio adicional no encontrado." });
                }

                _appDBContext.ServiciosAdicionales.Remove(servicio);
                await _appDBContext.SaveChangesAsync();
                return Ok(new { message = "Servicio eliminado exitosamente.", servicio });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "No se pudo eliminar el servicio" });
            }
        }
    }

    public class UtilsServiciosAdicionales
    {
        private readonly AppDBContext _appDBContext;
        public UtilsServiciosAdicionales(AppDBContext appDBContext)
        {
            _appDBContext = appDBContext;
        }

        //Funcion para validar todos los campos ingresados de los servicios adicionales
        public string ValidarServicio(ServiciosAdicionales servicio)
        {
            if (servicio.Descripcion == null)
            {
                return "La descripción es requerida.";
            }
            if (servicio.Costo == 0)
            {
                return "El precio es requerido.";
            }
            if (servicio.Descripcion.Length > 255)
            {
                return "La descripción no puede tener más de 255 caracteres.";
            }
            if (servicio.Costo < 0)
            {
                return "El costo no puede ser negativo.";
            }
            else if (servicio.Costo > 1000)
            {
                return "El costo no puede ser mayor a 1000.";
            }

            // Validar ingreso del idReserva
            if (servicio.ReservaId == 0)
            {
                return "La reserva es requerida.";
            }

            // Validar que la reserva exista
            var reserva = _appDBContext.Reserva.FirstOrDefault(r => r.Id == servicio.ReservaId);
            if (reserva == null)
            {
                return "La reserva no existe.";
            }

            // Comida, Transporte, Guia, Servicio a la habitacion con refil.
            if (servicio.Descripcion != "Comida" && servicio.Descripcion != "Transporte" && servicio.Descripcion != "Guia" && servicio.Descripcion != "Servicio a la habitacion")
            {
                return "El servicio adicional no es válido. Debe ser Comida, Transporte, Guia o Servicio a la habitacion";
            }

            return "";

        }
    }
}
