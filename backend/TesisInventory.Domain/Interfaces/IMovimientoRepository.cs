using System.Collections.Generic;
using System.Threading.Tasks;
using TesisInventory.Domain.Entities;

namespace TesisInventory.Domain.Interfaces
{
    public interface IMovimientoRepository
    {
        Task<IEnumerable<Movimiento>> GetMovimientosAsync(int idProducto, int idSede, string? tipoMovimiento, string? fechaDesde, string? fechaHasta);
        Task<(IEnumerable<Movimiento> Items, int TotalCount)> GetHistorialGlobalAsync(
            int idSede, 
            string? search = null, 
            int? idRubro = null, 
            int? idFamilia = null, 
            string? tipoMovimiento = null, 
            int? idUsuario = null, 
            string? fechaDesde = null, 
            string? fechaHasta = null, 
            int skip = 0, 
            int take = 50);
        Task<Movimiento> AddMovimientoAsync(Movimiento movimiento);
        Task<bool> HasAnyMovimientoByProductoAsync(int idProducto);
    }
}
