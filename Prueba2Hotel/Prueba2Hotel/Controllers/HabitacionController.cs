using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Prueba2Hotel.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class HabitacionController : ControllerBase
    {

        private readonly AppDBContext _appDBContext;

        public HabitacionController(AppDBContext appDBContext)
        {
            _appDBContext = appDBContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetHabitaciones()
        {
            return Ok(await _appDBContext.Habitacion.Select(r => new { r.Id, r.NumHabitacion, r.Tipo, r.NumMaximoPersonas, r.Descripcion, r.Estado}).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> PostHabitacion(Habitacion habitacion)
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

            UtilsHabitacion utilsHabitacion = new UtilsHabitacion(_appDBContext);

            string mensaje = utilsHabitacion.ValidarHabitacion(habitacion);

            if (mensaje != "")
            {
                return Ok(new { message = mensaje });
            }

            habitacion.Estado = "Disponible";

            _appDBContext.Habitacion.Add(habitacion);
            await _appDBContext.SaveChangesAsync();
            return Ok(habitacion);
        }

        [HttpPut("{NumHabitacion}")]
        public async Task<IActionResult> PutHabitacion(string NumHabitacion, Habitacion habitacion)
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
            if (NumHabitacion != habitacion.NumHabitacion)
            {
                return Ok(new { message = "El numero de la habitación no coincide." });
            }

            UtilsHabitacion utilsHabitacion = new UtilsHabitacion(_appDBContext);

            string mensaje = utilsHabitacion.ValidarHabitacion(habitacion);

            if (mensaje != "")
            {
                return Ok(new { message = mensaje });
            }

            // Mantener el valor de Numhabitacion porque no se deberia editar...
            habitacion.NumHabitacion = NumHabitacion;

            try
            {
                _appDBContext.Entry(habitacion).State = EntityState.Modified;
                await _appDBContext.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _appDBContext.Habitacion.AnyAsync(h => h.NumHabitacion == NumHabitacion))
                {
                    return Ok(new { message = "Habitación no encontrada." });
                }
                else
                {
                    return Ok(new { message = "Error al actualizar la habitacion." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpDelete("{NumHabitacion}")]
        public async Task<IActionResult> DeleteHabitacion(string NumHabitacion)
        {
            try
            {
                //buscar la habitacion por el numero de habitacion
                var habitacion = await _appDBContext.Habitacion.FirstOrDefaultAsync(h => h.NumHabitacion == NumHabitacion);
                if (habitacion == null)
                {
                    return Ok(new { message = "Habitación no encontrada." });
                }

                var id = habitacion.Id;

                // Buscar todas las reservas que tengan la habitacion a eliminar
                var reservas = await _appDBContext.Reserva.Where(r => r.HabitacionId == id).ToListAsync();

                // Reubicar las reservas a una habitacion que tenga el estado disponible
                string nuevaHabitacion = "";
                foreach (var reserva in reservas)
                {
                    var habitacionDisponible = await _appDBContext.Habitacion.FirstOrDefaultAsync(h => h.Estado == "Disponible");
                    if (habitacionDisponible != null)
                    {
                        reserva.HabitacionId = habitacionDisponible.Id;
                        reserva.NumHabitacion = habitacionDisponible.NumHabitacion;
                        _appDBContext.Entry(reserva).State = EntityState.Modified;
                        nuevaHabitacion = habitacionDisponible.NumHabitacion;
                    }
                    else
                    {
                        return BadRequest(new { message = "No hay habitaciones disponibles para reubicar las reservas. No se puede eliminar la habitacion." });
                    }
                }

                _appDBContext.Habitacion.Remove(habitacion);
                await _appDBContext.SaveChangesAsync();
                return Ok(new { message = "Habitación eliminada exitosamente. Se reasignara a la habitacion: " + nuevaHabitacion, habitacion });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }
    }

    public class UtilsHabitacion
    {
        private readonly AppDBContext _appDBContext;
        public UtilsHabitacion(AppDBContext appDBContext)
        {
            _appDBContext = appDBContext;
        }
        public string ValidarHabitacion(Habitacion habitacion)
        {
            string mensaje = "";

            if (habitacion.NumHabitacion == null)
            {
                return "El número de habitación es requerido.";
            }
            // Validar que NumMaximoPersonas sea un número
            if (!int.TryParse(habitacion.NumMaximoPersonas.ToString(), out int numMaximoPersonas))
            {
                Console.WriteLine(numMaximoPersonas);
                return "El número máximo de personas debe ser un número válido, por favor ingresar un numero, no una palabra.";
            }
            else if (habitacion.NumMaximoPersonas == 0)
            {
                return "El número máximo de personas es requerido.";
            }
            if (habitacion.Descripcion == null)
            {
                return "La descripción es requerida.";
            }
            if (habitacion.Tipo == null)
            {
                return "El tipo de habitación es requerido.";
            }

            // Validar que el número de habitación no exista
            var habitacionExistente = _appDBContext.Habitacion.FirstOrDefault(h => h.NumHabitacion == habitacion.NumHabitacion);
            if (habitacionExistente != null)
            {
                return "El número de habitación ya existe.";
            }

            // Validar el numero de personas
            if (habitacion.NumMaximoPersonas > 8 || habitacion.NumMaximoPersonas < 0 )
            {
                return "El número máximo de personas no puede ser mayor a 8 ni negativo.";
            }

            // validar las longitudes
            if (!long.TryParse(habitacion.NumHabitacion, out long result))
            {
                Console.WriteLine(result);
                return ("El numero de habitacion solo puede contener números.");
            }
            else if (habitacion.NumHabitacion.Length > 4)
            {
                return "El número de habitación no puede tener más de 4 digitos";
            }

            else if (habitacion.Descripcion.Length > 255)
            {
                return "La descripción no puede tener más de 255 caracteres.";
            }
            else if (habitacion.Tipo.Length > 100)
            {
                return "El tipo de habitación no puede tener más de 100 caracteres.";
            }

            // Validar que el tipo de habitacion este en Premium, Estandar o Economica
            if (habitacion.Tipo != "Premium" && habitacion.Tipo != "Estandar" && habitacion.Tipo != "Economica")
            {
                return "El tipo de habitación debe ser Premium, Estandar o Economica.";
            }

            return mensaje;
        }

    }
}
