using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TesisInventory.Domain.Entities;
using TesisInventory.Domain.Enums;
using TesisInventory.Domain.Interfaces;
using TesisInventory.Infrastructure.Persistence;

namespace TesisInventory.Infrastructure.Repositories
{
    public class MovimientoRepository : IMovimientoRepository
    {
        private readonly InventoryDbContext _context;

        public MovimientoRepository(InventoryDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Movimiento>> GetMovimientosAsync(int idProducto, int idSede, string? tipoMovimiento, string? fechaDesde, string? fechaHasta)
        {
            var query = _context.Movimiento
                .Include(m => m.Usuario)
                .Where(m => m.IdProducto == idProducto && m.IdSede == idSede)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(tipoMovimiento) && Enum.TryParse(typeof(TipoMovimiento), tipoMovimiento, true, out var parsedTipo))
            {
                query = query.Where(m => m.TipoMovimiento == (TipoMovimiento)parsedTipo);
            }

            if (!string.IsNullOrWhiteSpace(fechaDesde) && DateTime.TryParse(fechaDesde, out var dateDesde))
            {
                query = query.Where(m => m.Fecha >= dateDesde);
            }

            if (!string.IsNullOrWhiteSpace(fechaHasta) && DateTime.TryParse(fechaHasta, out var dateHasta))
            {
                query = query.Where(m => m.Fecha <= dateHasta);
            }

            return await query
                .OrderByDescending(m => m.Fecha)
                .ToListAsync();
        }

        public async Task<(IEnumerable<Movimiento> Items, int TotalCount)> GetHistorialGlobalAsync(
            int idSede,
            string? search = null,
            int? idRubro = null,
            int? idFamilia = null,
            string? tipoMovimiento = null,
            int? idUsuario = null,
            string? fechaDesde = null,
            string? fechaHasta = null,
            int skip = 0,
            int take = 50)
        {
            var query = _context.Movimiento
                .Include(m => m.Usuario)
                .Include(m => m.Producto)
                .ThenInclude(p => p!.Familia)
                .ThenInclude(f => f!.Rubro)
                .Where(m => m.IdSede == idSede)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(m => m.Producto!.Sku.Contains(search) || m.Producto.Nombre.Contains(search));
            }

            if (idRubro.HasValue)
            {
                query = query.Where(m => m.Producto!.Familia!.IdRubro == idRubro.Value);
            }

            if (idFamilia.HasValue)
            {
                query = query.Where(m => m.Producto!.IdFamilia == idFamilia.Value);
            }

            if (!string.IsNullOrWhiteSpace(tipoMovimiento) && Enum.TryParse(typeof(TipoMovimiento), tipoMovimiento, true, out var parsedTipo))
            {
                query = query.Where(m => m.TipoMovimiento == (TipoMovimiento)parsedTipo);
            }

            if (idUsuario.HasValue)
            {
                query = query.Where(m => m.IdUsuario == idUsuario.Value);
            }

            if (!string.IsNullOrWhiteSpace(fechaDesde) && DateTime.TryParse(fechaDesde, out var dateDesde))
            {
                query = query.Where(m => m.Fecha >= dateDesde);
            }

            if (!string.IsNullOrWhiteSpace(fechaHasta) && DateTime.TryParse(fechaHasta, out var dateHasta))
            {
                // Ajustamos para incluir todo ese día
                var endOfDay = dateHasta.Date.AddDays(1).AddTicks(-1);
                query = query.Where(m => m.Fecha <= endOfDay);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(m => m.Fecha)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Movimiento> AddMovimientoAsync(Movimiento movimiento)
        {
            _context.Movimiento.Add(movimiento);
            await _context.SaveChangesAsync();
            return movimiento;
        }

        public async Task<bool> HasAnyMovimientoByProductoAsync(int idProducto)
        {
            return await _context.Movimiento
                .AnyAsync(m => m.IdProducto == idProducto);
        }
    }
}
