using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Prueba2Hotel.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class ReservasController : ControllerBase
    {

        private readonly AppDBContext _appDBContext;

        public ReservasController(AppDBContext appDBContext)
        {
            _appDBContext = appDBContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetReservas()
        {
            // Generar el serializer de reservas para mostrar el id
            return Ok(await _appDBContext.Reserva.Select(r => new { r.Id, r.Entrada, r.Salida, r.Precio, r.CedulaCliente, r.NumHabitacion }).ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> PostReserva(Reserva reserva)
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

            if (reserva == null) { return Ok(new { message = "Los datos no coinciden con el modelo mostrado." }); }

            UtilsReservas utilsReservas = new UtilsReservas(_appDBContext);

            if (string.IsNullOrEmpty(reserva.CedulaCliente)) { return Ok(new { message = "La cédula del cliente es requerida." }); }
            if (string.IsNullOrEmpty(reserva.NumHabitacion)) { return Ok(new { message = "El número de habitación es requerido." }); }

            int idCliente = utilsReservas.ObtenerIdCliente(reserva.CedulaCliente, reserva);
            if (idCliente == 0) { return Ok(new { message = "Cliente no encontrado" }); }
            reserva.ClienteId = idCliente;

            int idHabitacion = utilsReservas.ObtenerIdHabitacion(reserva.NumHabitacion, reserva);
            if (idHabitacion == 0) { return Ok(new { message = "Habitacion no encontrada" }); }
            reserva.HabitacionId = idHabitacion;

            string mensaje = utilsReservas.ValidarReserva(reserva);
            if (mensaje != "") { return Ok(new { message = mensaje }); }

            // Poner la habitación en estado reservada
            var habitacion = await _appDBContext.Habitacion.FirstOrDefaultAsync(h => h.Id == reserva.HabitacionId);
            if (habitacion == null) { return Ok(new { message = "La habitación no existe." }); }
            habitacion.Estado = "Reservada";
            _appDBContext.Entry(habitacion).State = EntityState.Modified;
            await _appDBContext.SaveChangesAsync();

            _appDBContext.Reserva.Add(reserva);
            await _appDBContext.SaveChangesAsync();
            return Ok(reserva);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutReserva(int id, [FromBody] Reserva reserva)
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

            if (id != reserva.Id) { return Ok(new { message = "El ID de la reserva no coincide." }); }

            UtilsReservas utilsReservas = new UtilsReservas(_appDBContext);

            string mensaje = utilsReservas.ValidarReserva(reserva);
            if (mensaje != "") { return Ok(new { message = mensaje }); }

            // Validar que la habitación esté disponible
            var habitacion = await _appDBContext.Habitacion.FirstOrDefaultAsync(h => h.NumHabitacion == reserva.NumHabitacion);
            if (habitacion == null) { return Ok(new { message = "La habitación no existe." }); }

            // Validar que el cliente exista
            var cliente = await _appDBContext.Cliente.FirstOrDefaultAsync(c => c.Cedula == reserva.CedulaCliente);
            if (cliente == null) {  return Ok(new { message = "El cliente no existe." }); }

            try
            {
                _appDBContext.Entry(reserva).State = EntityState.Modified;
                await _appDBContext.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _appDBContext.Reserva.AnyAsync(r => r.Id == id)) { return Ok(new { message = "Reserva no encontrada." }); }
                else { return Ok(new { message = "No se pudo actualizar la Reserva." }); }
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "No se pudo actualizar la Reserva." });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReserva(int id)
        {
            try
            {
                var reserva = await _appDBContext.Reserva.FindAsync(id);
                if (reserva == null)
                {
                    return NotFound(new { message = "Reserva no encontrada." });
                }

                // Poner la habitación en estado disponible
                var habitacion = await _appDBContext.Habitacion.FirstOrDefaultAsync(h => h.Id == reserva.HabitacionId);
                if (habitacion == null)
                {
                    return Ok(new { message = "La habitación no existe." });
                }
                habitacion.Estado = "Disponible";
                _appDBContext.Entry(habitacion).State = EntityState.Modified;
                await _appDBContext.SaveChangesAsync();

                // Eliminar los servicios adicionales relacionados a la reserva
                var servicios = await _appDBContext.ServiciosAdicionales.Where(s => s.ReservaId == id).ToListAsync();
                foreach (var s in servicios)
                {
                    _appDBContext.ServiciosAdicionales.Remove(s);
                }

                _appDBContext.Reserva.Remove(reserva);
                await _appDBContext.SaveChangesAsync();
                return Ok(new { message = "Reserva eliminada exitosamente.", reserva });

            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "No se pudo eliminar la reserva" });
            }
        }
    }

    public class UtilsReservas
    {
        private readonly AppDBContext _appDBContext;
        public UtilsReservas(AppDBContext appDBContext)
        {
            _appDBContext = appDBContext;
        }

        // Funcion para validar todos los campos de la reserva
        public string ValidarReserva(Reserva reserva)
        {
            string mensaje = ValidarIngresoDatos(reserva);
            if (mensaje != "")
            {
                return mensaje;
            }

            // Validar que la fecha de entrada sea menor a la fecha de salida
            if (reserva.Entrada >= reserva.Salida)
            {
                return "La fecha de entrada debe ser menor a la fecha de salida.";
            }

            // Validar que la habitación no esté reservada en las fechas seleccionadas
            var reservas = _appDBContext.Reserva.Where(r => r.HabitacionId == reserva.HabitacionId).ToList();
            if (reservas.Count == 0)
            {
                return "";
            }
            var habitaciones = _appDBContext.Habitacion.Where(r => r.Estado == "Disponible").ToList();
            string habitacionesDisponibles = "";
            if (habitaciones.Count == 0)
            {
                habitacionesDisponibles = "No hay habitaciones disponibles";
            }
            else
            {
                habitacionesDisponibles += " Tenemos las siguientes habitaciones disponibles:";
                foreach (var h in habitaciones)
                {
                    habitacionesDisponibles += " " + h.NumHabitacion;
                }
            }
            foreach (var r in reservas)
            {
                if (reserva.Entrada >= r.Entrada && reserva.Entrada <= r.Salida)
                {
                    return "La habitación ya está reservada en la fecha de entrada." + habitacionesDisponibles;
                }
                if (reserva.Salida >= r.Entrada && reserva.Salida <= r.Salida)
                {
                    return "La habitación ya está reservada en la fecha de salida." + habitacionesDisponibles;
                }
            }

            // Validar que el precio no se exceda del limite
            if (reserva.Precio > 10000)
            {
                return "El precio no puede ser mayor a 10000.";
            }

            return "";
        }

        public static string ValidarIngresoDatos(Reserva reserva)
        {
            if (reserva.Entrada == null)
            {
                return "La fecha de entrada es requerida.";
            }
            if (reserva.Salida == null)
            {
                return "La fecha de salida es requerida.";
            }
            if (reserva.Precio == 0)
            {
                return "El precio es requerido.";
            }
            else if (reserva.Precio < 0)
            {
                return "El precio no puede ser negativo.";
            }
            return "";
        }

        public int ObtenerIdCliente(string cedula, Reserva reserva)
        {
            var cliente = _appDBContext.Cliente.FirstOrDefault(c => c.Cedula == reserva.CedulaCliente);
            if (cliente == null)
            {
                return 0;
            }
            return cliente.Id;
        }

        public int ObtenerIdHabitacion(string numHabitacion, Reserva reserva)
        {
            var habitacion = _appDBContext.Habitacion.FirstOrDefault(h => h.NumHabitacion == reserva.NumHabitacion);
            if (habitacion == null)
            {
                return 0;
            }
            return habitacion.Id;
        }

    }
}
