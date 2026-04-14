namespace TesisInventory.Domain.Entities
{
    public class AtributoOpcion
    {
        public int IdAtributoOpcion { get; set; }
        public int IdAtributo { get; set; }
        public string CodigoOpcion { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;

        // Propiedades de navegación
        public Atributo? Atributo { get; set; }
    }
}
