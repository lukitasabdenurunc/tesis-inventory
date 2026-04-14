using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TesisInventory.Application.DTOs.Stock;
using TesisInventory.Application.Interfaces;

namespace TesisInventory.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // [Authorize] // Asume que todos estos endpoints requieren autenticación
    public class StockController : ControllerBase
    {
        private readonly IStockService _stockService;

        public StockController(IStockService stockService)
        {
            _stockService = stockService;
        }

        // Obtener el ID del usuario autenticado desde el token JWT
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("id")?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            throw new UnauthorizedAccessException("Usuario no autenticado correctamente.");
        }

        // Obtener el ID de la sede del usuario (en un futuro esto podría venir del token o del front)
        // Por ahora, como regla: "el usuario puede pertenecer a varias sedes, pero visualiza la sede en contexto"
        // Simularemos que el ID de Sede se pasa como parámetro o se obtiene del claim "SedeId".
        private int GetCurrentSedeId()
        {
            int sedeClaimId = 0;
            var sedeIdClaim = User.FindFirst("sede_id")?.Value;
            if (int.TryParse(sedeIdClaim, out int sId))
            {
                sedeClaimId = sId;
            }

            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value ?? User.FindFirst("nombreRol")?.Value;
            bool isAdmin = (roleClaim == "Admin" || roleClaim == "Administrador");

            // Priorizar el Sede-Contexto que envía el front (Interceptor)
            if (Request.Headers.TryGetValue("Sede-Contexto", out var sedeContexto) && int.TryParse(sedeContexto, out int headerSedeId))
            {
                if (isAdmin) return headerSedeId; // Administradores pueden ver/actuar sobre cualquier sede seleccionada
                
                // Si no es admin, ignoramos el header si intenta spoofear y forzamos su sede
                if (headerSedeId == sedeClaimId) return headerSedeId;
            }

            if (sedeClaimId > 0) return sedeClaimId;

            throw new UnauthorizedAccessException("Debe especificar la Sede en Contexto.");
        }

        [HttpGet("sede")]
        public async Task<IActionResult> GetStockSede(
            [FromQuery] string? search = null,
            [FromQuery] int? idRubro = null,
            [FromQuery] int? idFamilia = null,
            [FromQuery] bool? estado = null,
            [FromQuery] bool? bajoStock = null,
            [FromQuery] int? idSedeQuery = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                int idSede = idSedeQuery ?? GetCurrentSedeId();
                int skip = (page - 1) * pageSize;

                var (items, totalCount) = await _stockService.GetStockAsync(
                    idSede, search, idRubro, idFamilia, estado, bajoStock, skip, pageSize);

                return Ok(new
                {
                    Data = items,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("incremento")]
        public async Task<IActionResult> IncrementarStock([FromBody] IncrementarStockDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                int idUsuario = GetCurrentUserId();
                int idSede = GetCurrentSedeId();

                var stockUpdate = await _stockService.IncrementarStockAsync(idSede, idUsuario, dto);
                return Ok(new { Message = "Stock incrementado con éxito", Data = stockUpdate });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("consumo")]
        public async Task<IActionResult> RegistrarConsumo([FromBody] RegistrarConsumoDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                int idUsuario = GetCurrentUserId();
                int idSede = GetCurrentSedeId();

                var stockUpdate = await _stockService.RegistrarConsumoAsync(idSede, idUsuario, dto);
                return Ok(new { Message = "Consumo registrado con éxito", Data = stockUpdate });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("transferencia")]
        public async Task<IActionResult> RegistrarTransferencia([FromBody] RegistrarTransferenciaDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                int idUsuario = GetCurrentUserId();
                int idSedeOrigen = GetCurrentSedeId();

                var transferencia = await _stockService.RegistrarTransferenciaAsync(idSedeOrigen, idUsuario, dto);
                return Ok(new { Message = "Transferencia solicitada con éxito", Data = transferencia });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("{idProducto}/movimientos")]
        public async Task<IActionResult> GetMovimientos(
            int idProducto,
            [FromQuery] string? tipoMovimiento = null,
            [FromQuery] string? fechaDesde = null,
            [FromQuery] string? fechaHasta = null)
        {
            try
            {
                int idSede = GetCurrentSedeId();
                var movimientos = await _stockService.GetHistorialMovimientosAsync(idProducto, idSede, tipoMovimiento, fechaDesde, fechaHasta);
                return Ok(movimientos);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("movimientos/historial")]
        public async Task<IActionResult> GetHistorialGlobal(
            [FromQuery] string? search = null,
            [FromQuery] int? idRubro = null,
            [FromQuery] int? idFamilia = null,
            [FromQuery] string? tipoMovimiento = null,
            [FromQuery] int? idUsuario = null,
            [FromQuery] string? fechaDesde = null,
            [FromQuery] string? fechaHasta = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                int idSede = GetCurrentSedeId();
                int skip = (page - 1) * pageSize;

                var (items, totalCount) = await _stockService.GetHistorialGlobalAsync(
                    idSede, search, idRubro, idFamilia, tipoMovimiento, idUsuario, fechaDesde, fechaHasta, skip, pageSize);

                return Ok(new
                {
                    Data = items,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
