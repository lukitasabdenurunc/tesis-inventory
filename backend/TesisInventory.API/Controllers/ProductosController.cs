using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TesisInventory.Application.DTOs.Productos;
using TesisInventory.Application.Interfaces;

namespace TesisInventory.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProductosController : ControllerBase
    {
        private readonly IProductosService _productosService;

        public ProductosController(IProductosService productosService)
        {
            _productosService = productosService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
        {
            var result = await _productosService.GetAllProductosAsync(includeInactive);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _productosService.GetProductoByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateProductoDto dto)
        {
            var result = await _productosService.CreateProductoAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.IdProducto }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateProductoDto dto)
        {
            var result = await _productosService.UpdateProductoAsync(id, dto);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] string confirmacionNombre)
        {
            try
            {
                await _productosService.DeleteProductoAsync(id, confirmacionNombre);
                return NoContent();
            }
            catch (InvalidOperationException ex) when (ex.Message.StartsWith("DESACTIVAR_SOLAMENTE"))
            {
                var mensaje = ex.Message.Split('|').Length > 1 ? ex.Message.Split('|')[1] : ex.Message;
                return Conflict(new { message = mensaje, code = "DESACTIVAR_SOLAMENTE" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
