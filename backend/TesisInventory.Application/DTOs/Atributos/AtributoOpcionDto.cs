namespace TesisInventory.Application.DTOs.Atributos
{
    public class AtributoOpcionDto
    {
        public int IdAtributoOpcion { get; set; }
        public int IdAtributo { get; set; }
        public string CodigoOpcion { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }
}
