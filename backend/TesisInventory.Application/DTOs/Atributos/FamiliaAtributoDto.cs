using System;

namespace TesisInventory.Application.DTOs.Atributos
{
    public class FamiliaAtributoDto
    {
        public int IdFamiliaAtributo { get; set; }
        public int IdFamilia { get; set; }
        public string NombreFamilia { get; set; } = string.Empty;
        public int IdAtributo { get; set; }
        public string NombreAtributo { get; set; } = string.Empty;
        public string TipoDatoAtributo { get; set; } = string.Empty;
        public bool Obligatorio { get; set; }
        public bool Activo { get; set; }
    }
}
