using System.ComponentModel.DataAnnotations;

namespace TesisInventory.Application.DTOs.Atributos
{
    public class CreateAtributoOpcionDto
    {
        [Required(ErrorMessage = "El código es obligatorio.")]
        public string CodigoOpcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "El valor es obligatorio.")]
        public string Valor { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;
    }
}
