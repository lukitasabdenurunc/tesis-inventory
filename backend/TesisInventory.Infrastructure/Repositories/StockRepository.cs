using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TesisInventory.Domain.Entities;
using TesisInventory.Domain.Interfaces;
using TesisInventory.Infrastructure.Persistence;

namespace TesisInventory.Infrastructure.Repositories
{
    public class StockRepository : IStockRepository
    {
        private readonly InventoryDbContext _context;

        public StockRepository(InventoryDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Stock>> GetStockBySedeAsync(
            int idSede,
            string? searchSkuOrName = null,
            int? idRubro = null,
            int? idFamilia = null,
            bool? estado = null,
            bool? bajoStock = null,
            int skip = 0,
            int take = 50)
        {
            var query = _context.Stock
                .Include(s => s.Producto)
                .ThenInclude(p => p!.Familia)
                .ThenInclude(f => f!.Rubro)
                .Where(s => s.IdSede == idSede)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchSkuOrName))
            {
                query = query.Where(s => EF.Functions.Like(s.Producto!.Sku, $"%{searchSkuOrName}%") || 
                                         EF.Functions.Like(s.Producto.Nombre, $"%{searchSkuOrName}%"));
            }

            if (idRubro.HasValue)
                query = query.Where(s => s.Producto!.Familia!.IdRubro == idRubro.Value);

            if (idFamilia.HasValue)
                query = query.Where(s => s.Producto!.IdFamilia == idFamilia.Value);

            if (estado.HasValue)
                query = query.Where(s => s.Producto!.Activo == estado.Value);

            if (bajoStock.HasValue && bajoStock.Value)
                query = query.Where(s => s.CantidadActual <= s.PuntoReposicion);

            return await query
                .OrderBy(s => s.Producto!.Nombre)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetTotalStockBySedeAsync(
            int idSede,
            string? searchSkuOrName = null,
            int? idRubro = null,
            int? idFamilia = null,
            bool? estado = null,
            bool? bajoStock = null)
        {
            var query = _context.Stock
                .Where(s => s.IdSede == idSede)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchSkuOrName))
            {
                query = query.Where(s => EF.Functions.Like(s.Producto!.Sku, $"%{searchSkuOrName}%") || 
                                         EF.Functions.Like(s.Producto.Nombre, $"%{searchSkuOrName}%"));
            }

            if (idRubro.HasValue)
                query = query.Where(s => s.Producto!.Familia!.IdRubro == idRubro.Value);

            if (idFamilia.HasValue)
                query = query.Where(s => s.Producto!.IdFamilia == idFamilia.Value);

            if (estado.HasValue)
                query = query.Where(s => s.Producto!.Activo == estado.Value);

            if (bajoStock.HasValue && bajoStock.Value)
                query = query.Where(s => s.CantidadActual <= s.PuntoReposicion);

            return await query.CountAsync();
        }

        public async Task<Stock?> GetStockAsync(int idProducto, int idSede)
        {
            return await _context.Stock
                .Include(s => s.Producto)
                .FirstOrDefaultAsync(s => s.IdProducto == idProducto && s.IdSede == idSede);
        }

        public async Task<Stock> AddStockAsync(Stock stock)
        {
            _context.Stock.Add(stock);
            await _context.SaveChangesAsync();
            return stock;
        }

        public async Task UpdateStockAsync(Stock stock)
        {
            _context.Stock.Update(stock);
            await _context.SaveChangesAsync();
        }

        public async Task UpdatePuntoReposicionAsync(int idProducto, int puntoReposicion)
        {
            await _context.Stock
                .Where(s => s.IdProducto == idProducto)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.PuntoReposicion, puntoReposicion));
        }

        public async Task<bool> HasAnyStockAsync(int idProducto)
        {
            return await _context.Stock
                .AnyAsync(s => s.IdProducto == idProducto && s.CantidadActual > 0);
        }

        public async Task<IEnumerable<Stock>> GetAllStockByProductoAsync(int idProducto)
        {
            return await _context.Stock
                .Where(s => s.IdProducto == idProducto)
                .ToListAsync();
        }

        public async Task RemoveStockAsync(Stock stock)
        {
            _context.Stock.Remove(stock);
            await _context.SaveChangesAsync();
        }
    }
}
