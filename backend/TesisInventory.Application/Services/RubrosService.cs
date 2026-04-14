using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TesisInventory.Application.DTOs.Rubros;
using TesisInventory.Application.Interfaces;
using TesisInventory.Domain.Entities;
using TesisInventory.Domain.Interfaces;

namespace TesisInventory.Application.Services
{
    public class RubrosService : IRubrosService
    {
        private readonly IRubroRepository _rubroRepository;

        public RubrosService(IRubroRepository rubroRepository)
        {
            _rubroRepository = rubroRepository;
        }

        public async Task<IEnumerable<RubroDto>> GetAllRubrosAsync(bool includeInactive = false)
        {
            var rubros = await _rubroRepository.GetAllAsync(includeInactive);
            return rubros.Select(r => new RubroDto
            {
                IdRubro = r.IdRubro,
                CodigoRubro = r.CodigoRubro,
                Nombre = r.Nombre,
                Activo = r.Activo,
                FechaCreacion = r.FechaCreacion,
                FechaActualizacion = r.FechaActualizacion
            });
        }

        public async Task<RubroDto?> GetRubroByIdAsync(int id)
        {
            var r = await _rubroRepository.GetByIdAsync(id);
            if (r == null) return null;

            return new RubroDto
            {
                IdRubro = r.IdRubro,
                CodigoRubro = r.CodigoRubro,
                Nombre = r.Nombre,
                Activo = r.Activo,
                FechaCreacion = r.FechaCreacion,
                FechaActualizacion = r.FechaActualizacion
            };
        }

        public async Task<RubroDto> CreateRubroAsync(CreateRubroDto createRubroDto)
        {
            var existing = await _rubroRepository.GetByCodigoAsync(createRubroDto.CodigoRubro);
            if (existing != null)
                throw new InvalidOperationException("Ya existe un rubro con este código.");

            var newRubro = new Rubro
            {
                CodigoRubro = createRubroDto.CodigoRubro,
                Nombre = createRubroDto.Nombre,
                Activo = createRubroDto.Activo,
                FechaCreacion = DateTime.UtcNow,
                FechaActualizacion = DateTime.UtcNow
            };

            await _rubroRepository.AddAsync(newRubro);

            return new RubroDto
            {
                IdRubro = newRubro.IdRubro,
                CodigoRubro = newRubro.CodigoRubro,
                Nombre = newRubro.Nombre,
                Activo = newRubro.Activo,
                FechaCreacion = newRubro.FechaCreacion,
                FechaActualizacion = newRubro.FechaActualizacion
            };
        }

        public async Task<RubroDto> UpdateRubroAsync(int id, UpdateRubroDto updateRubroDto)
        {
            var rubro = await _rubroRepository.GetByIdAsync(id);
            if (rubro == null)
                throw new KeyNotFoundException("Rubro no encontrado.");

            if (rubro.CodigoRubro != updateRubroDto.CodigoRubro)
            {
                var existing = await _rubroRepository.GetByCodigoAsync(updateRubroDto.CodigoRubro);
                if (existing != null)
                    throw new InvalidOperationException("Ya existe otro rubro con este código.");
            }

            bool wasActive = rubro.Activo;
            rubro.CodigoRubro = updateRubroDto.CodigoRubro;
            rubro.Nombre = updateRubroDto.Nombre;
            rubro.Activo = updateRubroDto.Activo;
            rubro.FechaActualizacion = DateTime.UtcNow;

            await _rubroRepository.UpdateAsync(rubro);

            // RF004: Si se desactivó el rubro, desactivar todas sus familias
            if (wasActive && !rubro.Activo)
            {
                await _rubroRepository.DeleteAsync(rubro); // El método DeleteAsync ya maneja la desactivación de familias en cascada
            }

            return new RubroDto
            {
                IdRubro = rubro.IdRubro,
                CodigoRubro = rubro.CodigoRubro,
                Nombre = rubro.Nombre,
                Activo = rubro.Activo,
                FechaCreacion = rubro.FechaCreacion,
                FechaActualizacion = rubro.FechaActualizacion
            };
        }

        public async Task DeleteRubroAsync(int id)
        {
            var rubro = await _rubroRepository.GetByIdAsync(id);
            if (rubro == null)
                throw new KeyNotFoundException("Rubro no encontrado.");

            // El requerimiento pide que "Una familia siempre debe estar asociada a un rubro existente"
            // Se asume que la baja logica desactiva el rubro. No borramos las familias, quizas deberiamos chequear dependencias activas o simplemente dar de baja lógica
            
            await _rubroRepository.DeleteAsync(rubro);
        }
    }
}
