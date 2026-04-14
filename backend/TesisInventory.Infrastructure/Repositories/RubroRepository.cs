using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TesisInventory.Domain.Entities;
using TesisInventory.Domain.Interfaces;
using TesisInventory.Infrastructure.Persistence;

namespace TesisInventory.Infrastructure.Repositories
{
    public class RubroRepository : IRubroRepository
    {
        private readonly InventoryDbContext _context;

        public RubroRepository(InventoryDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Rubro>> GetAllAsync(bool includeInactive = false)
        {
            var query = _context.Rubro.AsQueryable();
            if (!includeInactive)
            {
                query = query.Where(r => r.Activo);
            }
            return await query.ToListAsync();
        }

        public async Task<Rubro?> GetByIdAsync(int id)
        {
            return await _context.Rubro.FirstOrDefaultAsync(r => r.IdRubro == id);
        }

        public async Task<Rubro?> GetByCodigoAsync(string codigo)
        {
            return await _context.Rubro.FirstOrDefaultAsync(r => r.CodigoRubro == codigo);
        }

        public async Task<Rubro> AddAsync(Rubro rubro)
        {
            await _context.Rubro.AddAsync(rubro);
            await _context.SaveChangesAsync();
            return rubro;
        }

        public async Task UpdateAsync(Rubro rubro)
        {
            _context.Rubro.Update(rubro);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Rubro rubro)
        {
            rubro.Activo = false; // Baja lógica
            
            // RF004: Desactivar también las familias asociadas
            var familias = await _context.Familia.Where(f => f.IdRubro == rubro.IdRubro).ToListAsync();
            foreach (var familia in familias)
            {
                familia.Activo = false;
            }

            _context.Rubro.Update(rubro);
            await _context.SaveChangesAsync();
        }
    }
}
