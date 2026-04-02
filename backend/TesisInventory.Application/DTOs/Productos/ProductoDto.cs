using System;
using System.Collections.Generic;

namespace TesisInventory.Application.DTOs.Productos
{
    public class ProductoDto
    {
        public int IdProducto { get; set; }
        public int IdFamilia { get; set; }
        public string NombreFamilia { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public int PuntoReposicion { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaActualizacion { get; set; }

        public List<ProductoAtributoValorDto> Atributos { get; set; } = new List<ProductoAtributoValorDto>();
    }
}
