using System.Collections.Generic;
using System.Threading.Tasks;
using TesisInventory.Domain.Entities;

namespace TesisInventory.Domain.Interfaces
{
    public interface IStockRepository
    {
        Task<IEnumerable<Stock>> GetStockBySedeAsync(
            int idSede,
            string? searchSkuOrName = null,
            int? idRubro = null,
            int? idFamilia = null,
            bool? estado = null,
            bool? bajoStock = null,
            int skip = 0,
            int take = 50);

        Task<int> GetTotalStockBySedeAsync(
            int idSede,
            string? searchSkuOrName = null,
            int? idRubro = null,
            int? idFamilia = null,
            bool? estado = null,
            bool? bajoStock = null);

        Task<Stock?> GetStockAsync(int idProducto, int idSede);
        Task<Stock> AddStockAsync(Stock stock);
        Task UpdateStockAsync(Stock stock);
        Task UpdatePuntoReposicionAsync(int idProducto, int puntoReposicion);
        Task<bool> HasAnyStockAsync(int idProducto);
        Task<IEnumerable<Stock>> GetAllStockByProductoAsync(int idProducto);
        Task RemoveStockAsync(Stock stock);
    }
}
