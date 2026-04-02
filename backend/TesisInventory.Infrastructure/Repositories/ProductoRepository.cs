using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TesisInventory.Domain.Entities;
using TesisInventory.Domain.Interfaces;
using TesisInventory.Infrastructure.Persistence;

namespace TesisInventory.Infrastructure.Repositories
{
    public class ProductoRepository : IProductoRepository
    {
        private readonly InventoryDbContext _context;

        public ProductoRepository(InventoryDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Producto>> GetAllProductosAsync(bool includeInactive = false)
        {
            var query = _context.Producto
                .Include(p => p.Familia)
                .ThenInclude(f => f.Rubro)
                .Include(p => p.ProductoAtributoValores)
                .ThenInclude(pav => pav.Atributo)
                .Include(p => p.Stocks)
                .AsQueryable();

            if (!includeInactive)
                query = query.Where(p => p.Activo);

            return await query.ToListAsync();
        }

        public async Task<Producto?> GetProductoByIdAsync(int id)
        {
            return await _context.Producto
                .Include(p => p.Familia)
                .ThenInclude(f => f.Rubro)
                .Include(p => p.ProductoAtributoValores)
                .ThenInclude(pav => pav.Atributo)
                .Include(p => p.Stocks)
                .FirstOrDefaultAsync(p => p.IdProducto == id);
        }

        public async Task<Producto?> GetProductoBySkuAsync(string sku)
        {
            return await _context.Producto.FirstOrDefaultAsync(p => p.Sku == sku);
        }

        public async Task<Producto> AddProductoAsync(Producto producto)
        {
            await _context.Producto.AddAsync(producto);
            await _context.SaveChangesAsync();
            return producto;
        }

        public async Task UpdateProductoAsync(Producto producto)
        {
            _context.Producto.Update(producto);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteProductoAsync(Producto producto)
        {
            _context.Producto.Remove(producto);
            await _context.SaveChangesAsync();
        }

        // ====== Valores Atributos ======

        public async Task<IEnumerable<ProductoAtributoValor>> GetAtributosValorByProductoAsync(int idProducto)
        {
            return await _context.ProductoAtributoValor
                .Include(pav => pav.Atributo)
                .Where(pav => pav.IdProducto == idProducto)
                .ToListAsync();
        }

        public async Task AddProductoAtributoValorAsync(ProductoAtributoValor valor)
        {
            await _context.ProductoAtributoValor.AddAsync(valor);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateProductoAtributoValorAsync(ProductoAtributoValor valor)
        {
            _context.ProductoAtributoValor.Update(valor);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveProductoAtributoValorAsync(ProductoAtributoValor valor)
        {
            _context.ProductoAtributoValor.Remove(valor);
            await _context.SaveChangesAsync();
        }

        public async Task<ProductoAtributoValor?> GetAtributoValorAsync(int idProducto, int idAtributo)
        {
            return await _context.ProductoAtributoValor
                .FirstOrDefaultAsync(pav => pav.IdProducto == idProducto && pav.IdAtributo == idAtributo);
        }
    }
}
