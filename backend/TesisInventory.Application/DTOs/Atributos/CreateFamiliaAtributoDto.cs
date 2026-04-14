using System.ComponentModel.DataAnnotations;

namespace TesisInventory.Application.DTOs.Atributos
{
    public class CreateFamiliaAtributoDto
    {
        [Required]
        public int IdAtributo { get; set; }
        
        public bool Obligatorio { get; set; } = false;
        public bool Activo { get; set; } = true;
    }
}
