using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Prueba2Hotel.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class ClienteController : ControllerBase
    {

        private readonly AppDBContext _appDBContext;

        public ClienteController(AppDBContext appDBContext)
        {
            _appDBContext = appDBContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetClientes()
        {
            return Ok(await _appDBContext.Cliente.ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> PostCliente(Cliente cliente)
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

            string mensaje = UtilsCliente.ValidacionDatosCliente(cliente);

            if (mensaje != "")
            {
                return Ok(new { message = mensaje });
            }

            if (await _appDBContext.Cliente.AnyAsync(c => c.Cedula == cliente.Cedula))
            {
                return Ok(new { message = "Ya existe un cliente con esa cédula." });
            }
            if (await _appDBContext.Cliente.AnyAsync(c => c.Telefono == cliente.Telefono))
            {
                return Ok(new { message = "Ya existe un cliente con ese teléfono." });
            }

            _appDBContext.Cliente.Add(cliente);
            await _appDBContext.SaveChangesAsync();
            return Ok(cliente);
        }
        [HttpPut("{cedula}")]
        public async Task<IActionResult> PutCliente(string cedula, [FromBody] Cliente cliente)
        {
            if (!ModelState.IsValid)
            {
                // Errores de validación
                var errores = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return Ok(new { message = "Errores de validación", errores });
            }

            if (cedula != cliente.Cedula)
            {
                return Ok(new { message = "La cédula del cliente no coincide." });
            }

            string mensaje = UtilsCliente.ValidacionDatosCliente(cliente);
            if (!string.IsNullOrEmpty(mensaje))
            {
                return Ok(new { message = mensaje });
            }

            // Buscar cliente existente en la base de datos
            var clienteExistente = await _appDBContext.Cliente.FirstOrDefaultAsync(c => c.Cedula == cedula);
            if (clienteExistente == null)
            {
                return Ok(new { message = "Cliente no encontrado." });
            }

            // Actualizar los campos necesarios
            clienteExistente.Nombre = cliente.Nombre;
            clienteExistente.Apellido = cliente.Apellido;
            clienteExistente.Correo = cliente.Correo;
            clienteExistente.Telefono = cliente.Telefono;
            clienteExistente.Direccion = cliente.Direccion;

            try
            {
                _appDBContext.Cliente.Update(clienteExistente);
                await _appDBContext.SaveChangesAsync();
                return Ok(new { message = "Cliente actualizado exitosamente.", cliente = clienteExistente });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Ok(new { message = "Error al actualizar el cliente." });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpDelete("{cedula}")]
        public async Task<IActionResult> DeleteCliente(string cedula)
        {
            var cliente = await _appDBContext.Cliente.FirstOrDefaultAsync(c => c.Cedula == cedula);
            if (cliente == null)
            {
                return Ok(new { message = "Cliente no encontrado." });
            }
            
            var reservasRelacionadas = await _appDBContext.Reserva.AnyAsync(r => r.CedulaCliente == cedula);
            if (reservasRelacionadas)
            {
                return Ok(new { message = "No se puede eliminar el cliente porque tiene reservas." });
            }
            _appDBContext.Cliente.Remove(cliente);
            await _appDBContext.SaveChangesAsync();
            return Ok(cliente);
        }
    }

    public static partial class UtilsCliente
    {

        [GeneratedRegex(@"^[a-zA-Z\s]+$")]
        private static partial Regex LetrasYEspaciosRegex();

        [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,7}$")]
        private static partial Regex EmailValidationRegex();

        public static bool ValidarCedula(string cedula)
        {
            if (cedula.Length != 10)
            {
                return true;
            }
            int[] coeficientes = { 2, 1, 2, 1, 2, 1, 2, 1, 2 };
            int verificador = int.Parse(cedula.Substring(9, 1));
            int suma = 0;
            for (int i = 0; i < 9; i++)
            {
                int valor = int.Parse(cedula.Substring(i, 1)) * coeficientes[i];
                suma += (valor >= 10) ? valor - 9 : valor;
            }
            int residuo = suma % 10;
            int resultado = (residuo == 0) ? 0 : 10 - residuo;
            return (resultado != verificador);
        }

        public static string ValidacionDatosCliente(Cliente cliente )
        {

            string mensaje = "";

            mensaje = ValidarIngresoDatos(cliente);

            if (mensaje != "")
            {
                return mensaje;
            }

            if (cliente.Cedula.Length != 10)
            {
                return ("La cédula debe tener 10 dígitos.");
            }
            else if (ValidarCedula(cliente.Cedula))
            {
                return ("La cédula no es válida.");
            }
            else if (!long.TryParse(cliente.Cedula, out long result))
            {
                Console.WriteLine(result);
                return ("El teléfono solo puede contener números.");
            }

            // Validar que no se ingresen caracteres especiales en el nombre y apellido
            if (!LetrasYEspaciosRegex().IsMatch(cliente.Nombre))
            {
                return "El nombre solo puede contener letras.";
            }

            if (!LetrasYEspaciosRegex().IsMatch(cliente.Apellido))
            {
                return "El apellido solo puede contener letras.";
            }

            // Validar que el correo
            bool correoValido = EmailValidationRegex().IsMatch(cliente.Correo);

            if (!correoValido){ return "El correo no es valido."; }


            //Validar telefono
            if (cliente.Telefono.Length != 10)
            {
                return ("El teléfono debe tener 10 dígitos.");
            }
            else if (!long.TryParse(cliente.Telefono, out long result))
            {
                Console.WriteLine(result);
                return ("El teléfono solo puede contener números.");
            }

            return mensaje;
        }

        public static string ValidarIngresoDatos(Cliente cliente)
        {
            if (cliente.Cedula == null)
            {
                return ("La cédula es requerida.");
            }
            if (cliente.Nombre == null)
            {
                return ("El nombre es requerido.");
            }
            if (cliente.Apellido == null)
            {
                return ("El apellido es requerido.");
            }
            if (cliente.Correo == "")
            {
                return ("El correo es requerido.");
            }
            if (cliente.Telefono == null)
            {
                return ("El teléfono es requerido.");
            }
            if (cliente.Direccion == null)
            {
                return ("La dirección es requerida.");
            }

            if (cliente.Nombre.Length > 100)
            {
                return ("El nombre no puede tener más de 100 caracteres." );
            }
            else if (cliente.Apellido.Length > 100)
            {
                return ("El apellido no puede tener más de 100 caracteres." );
            }
            else if (cliente.Correo.Length > 100)
            {
                return ("El correo no puede tener más de 100 caracteres." );
            }
            else if (cliente.Direccion.Length > 100)
            {
                return ("La dirección no puede tener más de 100 caracteres." );
            }

            return "";
        }
    }


}
