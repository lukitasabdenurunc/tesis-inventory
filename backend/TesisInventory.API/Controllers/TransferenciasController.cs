using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using TesisInventory.Application.DTOs.Transferencias;
using TesisInventory.Application.Interfaces;

namespace TesisInventory.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TransferenciasController : ControllerBase
    {
        private readonly ITransferenciaService _transferenciaService;

        public TransferenciasController(ITransferenciaService transferenciaService)
        {
            _transferenciaService = transferenciaService;
        }

        private int GetCurrentUserId()
        {
            var userIdStr = User.FindFirstValue("id");
            return int.TryParse(userIdStr, out var id) ? id : 0;
        }

        private int GetCurrentUserSedeId()
        {
            int sedeClaimId = 0;
            var sedeIdStr = User.FindFirstValue("sede_id");
            if (int.TryParse(sedeIdStr, out var id))
            {
                sedeClaimId = id;
            }

            var roleClaim = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? User.FindFirstValue("nombreRol");
            bool isAdmin = (roleClaim == "Admin" || roleClaim == "Administrador");

            // Priorizar el Sede-Contexto que envía el front (Interceptor)
            if (Request.Headers.TryGetValue("Sede-Contexto", out var sedeContexto) && int.TryParse(sedeContexto, out int headerSedeId))
            {
                if (isAdmin) return headerSedeId;
                if (headerSedeId == sedeClaimId) return headerSedeId;
            }
            
            if (sedeClaimId > 0) return sedeClaimId;

            return 0;
        }

        [HttpGet("entrantes")]
        public async Task<IActionResult> GetEntrantes()
        {
            var sedeId = GetCurrentUserSedeId();
            if (sedeId == 0) return BadRequest("Sede no identificada en el token");

            var result = await _transferenciaService.GetEntrantesAsync(sedeId);
            return Ok(result);
        }

        [HttpGet("salientes")]
        public async Task<IActionResult> GetSalientes()
        {
            var sedeId = GetCurrentUserSedeId();
            if (sedeId == 0) return BadRequest("Sede no identificada en el token");

            var result = await _transferenciaService.GetSalientesAsync(sedeId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTransferenciaDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            var sedeIdContext = GetCurrentUserSedeId();

            if (userId == 0) return Unauthorized();

            // En este nuevo paradigma, el usuario que crea la solicitud es quien va a RECIBIR la mercadería.
            // Por lo tanto, su sede es la Sede Destino.
            dto.IdSedeDestino = sedeIdContext;

            try 
            {
                var result = await _transferenciaService.CreateAsync(dto, userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Enviar el error a la UI temporalmente para depuración
                return StatusCode(500, new { message = ex.Message, details = ex.InnerException?.Message ?? ex.StackTrace });
            }
        }

        [HttpPut("{id}/aceptar")]
        public async Task<IActionResult> Aceptar(int id, [FromBody] ResolverTransferenciaDto dto)
        {
            var userId = GetCurrentUserId();
            await _transferenciaService.AceptarTransferenciaAsync(id, userId, dto.Observaciones);
            return Ok(new { message = "Transferencia aceptada y en tránsito" });
        }

        [HttpPut("{id}/rechazar")]
        public async Task<IActionResult> Rechazar(int id, [FromBody] ResolverTransferenciaDto dto)
        {
            var userId = GetCurrentUserId();
            await _transferenciaService.RechazarTransferenciaAsync(id, userId, dto.Observaciones);
            return Ok(new { message = "Transferencia rechazada" });
        }

        [HttpPut("{id}/confirmar-recepcion")]
        public async Task<IActionResult> ConfirmarRecepcion(int id, [FromBody] ResolverTransferenciaDto dto)
        {
            var userId = GetCurrentUserId();
            await _transferenciaService.ConfirmarRecepcionAsync(id, userId, dto.Observaciones);
            return Ok(new { message = "Recepción confirmada" });
        }

        [HttpPut("{id}/devolver")]
        public async Task<IActionResult> Devolver(int id, [FromBody] ResolverTransferenciaDto dto)
        {
            var userId = GetCurrentUserId();
            await _transferenciaService.DevolverPrestamoAsync(id, userId, dto.Observaciones);
            return Ok(new { message = "Préstamo devuelto correctamente" });
        }
    }

    public class ResolverTransferenciaDto
    {
        public string? Observaciones { get; set; }
    }
}
