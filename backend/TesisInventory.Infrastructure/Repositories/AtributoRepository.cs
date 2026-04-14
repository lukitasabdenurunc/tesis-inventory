using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TesisInventory.Domain.Entities;
using TesisInventory.Domain.Interfaces;
using TesisInventory.Infrastructure.Persistence;

namespace TesisInventory.Infrastructure.Repositories
{
    public class AtributoRepository : IAtributoRepository
    {
        private readonly InventoryDbContext _context;

        public AtributoRepository(InventoryDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Atributo>> GetAllAtributosAsync(bool includeInactive = false)
        {
            var query = _context.Atributo.Include(a => a.Opciones).AsQueryable();
            if (!includeInactive)
                query = query.Where(a => a.Activo);

            return await query.ToListAsync();
        }

        public async Task<Atributo?> GetAtributoByIdAsync(int id)
        {
            return await _context.Atributo
                .Include(a => a.Opciones)
                .FirstOrDefaultAsync(a => a.IdAtributo == id);
        }

        public async Task<Atributo?> GetAtributoByCodigoAsync(string codigo)
        {
            return await _context.Atributo.FirstOrDefaultAsync(a => a.CodigoAtributo == codigo);
        }

        public async Task<Atributo> AddAtributoAsync(Atributo atributo)
        {
            await _context.Atributo.AddAsync(atributo);
            await _context.SaveChangesAsync();
            return atributo;
        }

        public async Task UpdateAtributoAsync(Atributo atributo)
        {
            _context.Atributo.Update(atributo);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAtributoAsync(Atributo atributo)
        {
            // ATR005 & ATR006: Cambiamos borrado físico por baja lógica
            // Esto evita errores 500 por integridad referencial y permite que el atributo quede inactivo en lugar de desaparecer.
            atributo.Activo = false;

            // Desactivamos también las relaciones para ser consistentes y evitar problemas de integridad referencial lógica
            var opciones = await _context.AtributoOpcion.Where(o => o.IdAtributo == atributo.IdAtributo).ToListAsync();
            foreach (var o in opciones) o.Activo = false;

            var asignaciones = await _context.FamiliaAtributo.Where(fa => fa.IdAtributo == atributo.IdAtributo).ToListAsync();
            foreach (var fa in asignaciones) fa.Activo = false;

            _context.Atributo.Update(atributo);
            await _context.SaveChangesAsync();
        }

        // ====== Opciones ======

        public async Task<IEnumerable<AtributoOpcion>> GetOpcionesByAtributoIdAsync(int idAtributo)
        {
            return await _context.AtributoOpcion
                .Where(o => o.IdAtributo == idAtributo && o.Activo)
                .ToListAsync();
        }

        public async Task<AtributoOpcion> AddOpcionAsync(AtributoOpcion opcion)
        {
            await _context.AtributoOpcion.AddAsync(opcion);
            await _context.SaveChangesAsync();
            return opcion;
        }

        public async Task UpdateOpcionAsync(AtributoOpcion opcion)
        {
            _context.AtributoOpcion.Update(opcion);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteOpcionAsync(AtributoOpcion opcion)
        {
            // Borrado fisico si no tiene data, o borrado logico (ponemos fisico por simplicidad de opciones)
            _context.AtributoOpcion.Remove(opcion);
            await _context.SaveChangesAsync();
        }

        public async Task<AtributoOpcion?> GetOpcionByIdAsync(int idOpcion)
        {
            return await _context.AtributoOpcion.FirstOrDefaultAsync(o => o.IdAtributoOpcion == idOpcion);
        }

        // ====== Familia Atributo ======

        public async Task<IEnumerable<FamiliaAtributo>> GetAtributosByFamiliaIdAsync(int idFamilia)
        {
            return await _context.FamiliaAtributo
                .Include(fa => fa.Atributo)
                .ThenInclude(a => a.Opciones)
                .Where(fa => fa.IdFamilia == idFamilia && fa.Activo)
                .ToListAsync();
        }

        public async Task<FamiliaAtributo> AddFamiliaAtributoAsync(FamiliaAtributo familiaAtributo)
        {
            await _context.FamiliaAtributo.AddAsync(familiaAtributo);
            await _context.SaveChangesAsync();
            return familiaAtributo;
        }

        public async Task UpdateFamiliaAtributoAsync(FamiliaAtributo familiaAtributo)
        {
            _context.FamiliaAtributo.Update(familiaAtributo);
            await _context.SaveChangesAsync();
        }

        public async Task<FamiliaAtributo?> GetFamiliaAtributoAsync(int idFamilia, int idAtributo)
        {
            return await _context.FamiliaAtributo
                .FirstOrDefaultAsync(fa => fa.IdFamilia == idFamilia && fa.IdAtributo == idAtributo);
        }

        public async Task DeleteFamiliaAtributoAsync(FamiliaAtributo familiaAtributo)
        {
            _context.FamiliaAtributo.Remove(familiaAtributo);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<FamiliaAtributo>> GetFamiliasByAtributoIdAsync(int idAtributo)
        {
            return await _context.FamiliaAtributo
                .Include(fa => fa.Familia)
                .Where(fa => fa.IdAtributo == idAtributo && fa.Activo)
                .OrderBy(fa => fa.Familia!.Nombre)
                .ToListAsync();
        }
    }
}
