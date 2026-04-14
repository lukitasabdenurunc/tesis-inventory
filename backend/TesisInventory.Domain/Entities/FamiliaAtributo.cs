using System;

namespace TesisInventory.Domain.Entities
{
    public class FamiliaAtributo
    {
        public int IdFamiliaAtributo { get; set; }
        public int IdFamilia { get; set; }
        public int IdAtributo { get; set; }
        public bool Obligatorio { get; set; } = false;
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;

        // Propiedades de navegación
        public Familia? Familia { get; set; }
        public Atributo? Atributo { get; set; }
    }
}
