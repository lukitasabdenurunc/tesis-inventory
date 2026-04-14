using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TesisInventory.Application.DTOs.Productos;
using TesisInventory.Application.Interfaces;
using TesisInventory.Domain.Entities;
using TesisInventory.Domain.Interfaces;

namespace TesisInventory.Application.Services
{
    public class ProductosService : IProductosService
    {
        private readonly IProductoRepository _productoRepository;
        private readonly IFamiliaRepository _familiaRepository;
        private readonly IAtributoRepository _atributoRepository;
        private readonly IRubroRepository _rubroRepository;
        private readonly ISedeRepository _sedeRepository;
        private readonly IStockRepository _stockRepository;
        private readonly IMovimientoRepository _movimientoRepository;

        public ProductosService(
            IProductoRepository productoRepository,
            IFamiliaRepository familiaRepository,
            IAtributoRepository atributoRepository,
            IRubroRepository rubroRepository,
            ISedeRepository sedeRepository,
            IStockRepository stockRepository,
            IMovimientoRepository movimientoRepository)
        {
            _productoRepository = productoRepository;
            _familiaRepository = familiaRepository;
            _atributoRepository = atributoRepository;
            _rubroRepository = rubroRepository;
            _sedeRepository = sedeRepository;
            _stockRepository = stockRepository;
            _movimientoRepository = movimientoRepository;
        }

        public async Task<IEnumerable<ProductoDto>> GetAllProductosAsync(bool includeInactive = false)
        {
            var productos = await _productoRepository.GetAllProductosAsync(includeInactive);
            return productos.Select(MapToDto);
        }

        public async Task<ProductoDto?> GetProductoByIdAsync(int id)
        {
            var p = await _productoRepository.GetProductoByIdAsync(id);
            if (p == null) return null;
            return MapToDto(p);
        }

        public async Task<ProductoDto> CreateProductoAsync(CreateProductoDto createDto)
        {
            var familia = await _familiaRepository.GetByIdAsync(createDto.IdFamilia);
            if (familia == null || familia.IdRubro != createDto.IdRubro)
                throw new InvalidOperationException("Familia o rubro inválido.");

            // Validar Atributos Obligatorios
            var atributosFamilia = await _atributoRepository.GetAtributosByFamiliaIdAsync(familia.IdFamilia);
            var obligatorios = atributosFamilia.Where(fa => fa.Obligatorio).Select(fa => fa.IdAtributo).ToList();

            var providedAttrsIds = createDto.Atributos.Select(a => a.IdAtributo).ToList();
            var missing = obligatorios.Except(providedAttrsIds).ToList();
            
            if (missing.Any())
                throw new InvalidOperationException($"Faltan atributos obligatorios para esta familia (Ids: {string.Join(", ", missing)}).");

            // Extraer y procesar valores para los atributos obligatorios a usar en el SKU
            var skuAttributesValues = new List<string>();

            // Para mantener el orden, usamos la lista "obligatorios" como referencia de Ids si tiene algún orden específico 
            // aunque para mayor precisión usamos la asignación con la familia si hubiese orden ("fa.Orden")
            var obligatoriosOrdenados = atributosFamilia
                .Where(fa => fa.Obligatorio)
                .OrderBy(fa => fa.IdFamiliaAtributo)
                .ToList();

            foreach (var fa in obligatoriosOrdenados)
            {
                var inputAttr = createDto.Atributos.FirstOrDefault(a => a.IdAtributo == fa.IdAtributo);
                if (inputAttr == null) continue; // Ya fue validado arriba teóricamente

                // Detectar qué campo tiene el valor en base a lo que se envió en el DTO (como no tenemos directo TipoDato aquí)
                string attrVal = "";

                if (!string.IsNullOrWhiteSpace(inputAttr.ValorTexto)) 
                    attrVal = inputAttr.ValorTexto.Trim();
                else if (inputAttr.ValorNumero.HasValue) 
                    attrVal = inputAttr.ValorNumero.Value.ToString();
                else if (inputAttr.ValorDecimal.HasValue) 
                    attrVal = inputAttr.ValorDecimal.Value.ToString("0.##");
                else if (!string.IsNullOrWhiteSpace(inputAttr.ValorLista)) 
                    attrVal = inputAttr.ValorLista.Trim();
                else if (inputAttr.ValorBool.HasValue) 
                    attrVal = inputAttr.ValorBool.Value ? "SI" : "NO";

                // Limpiamos los espacios vacíos y agregamos
                if (!string.IsNullOrEmpty(attrVal))
                {
                    skuAttributesValues.Add(attrVal.Replace(" ", "")); // Sin espacios
                }
            }

            // Auto-generación del SKU: RubroCodigo-FamiliaCodigo-[Attr1]-[Attr2]-Numero
            var rubro = familia.Rubro ?? await _rubroRepository.GetByIdAsync(familia.IdRubro);
            
            var codigoBase = $"{rubro!.CodigoRubro}-{familia.CodigoFamilia}";
            
            if (skuAttributesValues.Any())
            {
                codigoBase += "-" + string.Join("-", skuAttributesValues);
            }
            // Agregamos el separador final antes del dígito
            codigoBase += "-";

            // Conseguir el correlativo actual para esta Familia
            var getAllProd = await _productoRepository.GetAllProductosAsync(true);
            var totalProductosEnFamilia = getAllProd.Count(p => p.IdFamilia == familia.IdFamilia);
            
            var nuevoSku = $"{codigoBase}{(totalProductosEnFamilia + 1).ToString().PadLeft(4, '0')}";

            var producto = new Producto
            {
                IdFamilia = createDto.IdFamilia,
                Sku = nuevoSku,
                Nombre = createDto.Nombre,
                UnidadMedida = createDto.UnidadMedida,
                Activo = createDto.Activo,
                FechaCreacion = DateTime.UtcNow,
                FechaActualizacion = DateTime.UtcNow
            };

            await _productoRepository.AddProductoAsync(producto);

            foreach (var attrDto in createDto.Atributos)
            {
                // Validación básica de que el atributo corresponde a la familia (opcional pero recomendada)
                if (!atributosFamilia.Any(fa => fa.IdAtributo == attrDto.IdAtributo))
                    continue; // Ignorar atributos no asociados

                var pav = new ProductoAtributoValor
                {
                    IdProducto = producto.IdProducto,
                    IdAtributo = attrDto.IdAtributo,
                    ValorTexto = attrDto.ValorTexto,
                    ValorNumero = attrDto.ValorNumero,
                    ValorDecimal = attrDto.ValorDecimal,
                    ValorBool = attrDto.ValorBool,
                    ValorLista = attrDto.ValorLista,
                    FechaActualizacion = DateTime.UtcNow
                };
                await _productoRepository.AddProductoAtributoValorAsync(pav);
            }

            // Inicializar stock en 0 para todas las sedes
            var sedes = await _sedeRepository.GetAllAsync();
            foreach (var s in sedes)
            {
                var stockInicial = new Stock
                {
                    IdProducto = producto.IdProducto,
                    IdSede = s.IdSede,
                    CantidadActual = 0,
                    PuntoReposicion = createDto.PuntoReposicion, // Generado desde el frontend
                    FechaActualizacion = DateTime.UtcNow
                };
                await _stockRepository.AddStockAsync(stockInicial);
            }

            return await GetProductoByIdAsync(producto.IdProducto) ?? throw new Exception("Error al obtener el producto creado.");
        }

        public async Task<ProductoDto> UpdateProductoAsync(int id, UpdateProductoDto updateDto)
        {
            var producto = await _productoRepository.GetProductoByIdAsync(id);
            if (producto == null) throw new KeyNotFoundException("Producto no encontrado.");

            producto.Nombre = updateDto.Nombre;
            producto.UnidadMedida = updateDto.UnidadMedida;
            producto.Activo = updateDto.Activo;
            producto.FechaActualizacion = DateTime.UtcNow;

            // Validar si quitaron alguno obligatorio
            var atributosFamilia = await _atributoRepository.GetAtributosByFamiliaIdAsync(producto.IdFamilia);
            var obligatorios = atributosFamilia.Where(af => af.Obligatorio).Select(af => af.IdAtributo).ToList();
            var guardados = updateDto.Atributos.Select(a => a.IdAtributo).ToList();
            
            var missing = obligatorios.Except(guardados).ToList();
            if (missing.Any())
                throw new InvalidOperationException("Faltan atributos obligatorios al actualizar.");

            // Recalcular SKU con los nuevos atributos
            var obligatoriosOrdenados = atributosFamilia
                .Where(fa => fa.Obligatorio)
                .OrderBy(fa => fa.IdFamiliaAtributo)
                .ToList();

            var skuAttributesValues = new List<string>();
            foreach (var fa in obligatoriosOrdenados)
            {
                var inputAttr = updateDto.Atributos.FirstOrDefault(a => a.IdAtributo == fa.IdAtributo);
                if (inputAttr == null) continue;

                string attrVal = "";

                if (!string.IsNullOrWhiteSpace(inputAttr.ValorTexto)) 
                    attrVal = inputAttr.ValorTexto.Trim();
                else if (inputAttr.ValorNumero.HasValue) 
                    attrVal = inputAttr.ValorNumero.Value.ToString();
                else if (inputAttr.ValorDecimal.HasValue) 
                    attrVal = inputAttr.ValorDecimal.Value.ToString("0.##");
                else if (!string.IsNullOrWhiteSpace(inputAttr.ValorLista)) 
                    attrVal = inputAttr.ValorLista.Trim();
                else if (inputAttr.ValorBool.HasValue) 
                    attrVal = inputAttr.ValorBool.Value ? "SI" : "NO";

                if (!string.IsNullOrEmpty(attrVal))
                {
                    skuAttributesValues.Add(attrVal.Replace(" ", ""));
                }
            }

            var familia = await _familiaRepository.GetByIdAsync(producto.IdFamilia);
            var rubro = familia!.Rubro ?? await _rubroRepository.GetByIdAsync(familia.IdRubro);
            
            var codigoBase = $"{rubro!.CodigoRubro}-{familia.CodigoFamilia}";
            if (skuAttributesValues.Any())
            {
                codigoBase += "-" + string.Join("-", skuAttributesValues);
            }
            codigoBase += "-";

            var parts = producto.Sku.Split('-');
            var correlativo = parts.Last(); 
            producto.Sku = $"{codigoBase}{correlativo}";

            await _productoRepository.UpdateProductoAsync(producto);
            await _stockRepository.UpdatePuntoReposicionAsync(producto.IdProducto, updateDto.PuntoReposicion);

            var atributosExistentes = await _productoRepository.GetAtributosValorByProductoAsync(id);

            // Actualizar o crear valores de atributos
            foreach (var reqAttr in updateDto.Atributos)
            {
                var fa = await _atributoRepository.GetFamiliaAtributoAsync(producto.IdFamilia, reqAttr.IdAtributo);
                if (fa == null) continue; // Si el atributo ya no es de la familia, lo saltamos (o podríamos lanzar error)

                var existingVal = atributosExistentes.FirstOrDefault(pav => pav.IdAtributo == reqAttr.IdAtributo);
                
                if (existingVal != null)
                {
                    existingVal.ValorTexto = reqAttr.ValorTexto;
                    existingVal.ValorNumero = reqAttr.ValorNumero;
                    existingVal.ValorDecimal = reqAttr.ValorDecimal;
                    existingVal.ValorBool = reqAttr.ValorBool;
                    existingVal.ValorLista = reqAttr.ValorLista;
                    existingVal.FechaActualizacion = DateTime.UtcNow;
                    await _productoRepository.UpdateProductoAtributoValorAsync(existingVal);
                }
                else
                {
                    var newVal = new ProductoAtributoValor
                    {
                        IdProducto = id,
                        IdAtributo = reqAttr.IdAtributo,
                        ValorTexto = reqAttr.ValorTexto,
                        ValorNumero = reqAttr.ValorNumero,
                        ValorDecimal = reqAttr.ValorDecimal,
                        ValorBool = reqAttr.ValorBool,
                        ValorLista = reqAttr.ValorLista,
                        FechaActualizacion = DateTime.UtcNow
                    };
                    await _productoRepository.AddProductoAtributoValorAsync(newVal);
                }
            }


            return await GetProductoByIdAsync(producto.IdProducto) ?? throw new Exception("Error post update");
        }

        public async Task DeleteProductoAsync(int id, string confirmacionNombre)
        {
            var producto = await _productoRepository.GetProductoByIdAsync(id);
            if (producto == null) throw new KeyNotFoundException("Producto no encontrado.");

            if (!producto.Nombre.Equals(confirmacionNombre, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("El nombre de confirmación no coincide exactamente con el producto.");
            }

            // Verificar si tiene stock > 0 en alguna sede o movimientos en el historial
            var tieneStock = await _stockRepository.HasAnyStockAsync(id);
            var tieneMovimientos = await _movimientoRepository.HasAnyMovimientoByProductoAsync(id);

            if (tieneStock || tieneMovimientos)
            {
                // Solo puede ser desactivado, no eliminado
                throw new InvalidOperationException(
                    "DESACTIVAR_SOLAMENTE|Este producto tiene " +
                    (tieneStock ? "stock activo" : "") +
                    (tieneStock && tieneMovimientos ? " y " : "") +
                    (tieneMovimientos ? "movimientos en el historial" : "") +
                    ". No se puede eliminar, únicamente puede ser desactivado.");
            }

            // Si no tiene stock ni movimientos, eliminar físicamente
            // Primero eliminar registros de stock (con cantidad 0)
            var stockRecords = await _stockRepository.GetAllStockByProductoAsync(id);
            foreach (var stock in stockRecords)
            {
                await _stockRepository.RemoveStockAsync(stock);
            }

            // Eliminar valores de atributos
            var atributos = await _productoRepository.GetAtributosValorByProductoAsync(id);
            foreach (var attr in atributos)
            {
                await _productoRepository.RemoveProductoAtributoValorAsync(attr);
            }

            // Finalmente eliminar el producto
            await _productoRepository.DeleteProductoAsync(producto);
        }

        public async Task RegenerateSkusForFamiliaAsync(int idFamilia)
        {
            var todosProductos = await _productoRepository.GetAllProductosAsync(true);
            var productosFamilia = todosProductos.Where(p => p.IdFamilia == idFamilia).ToList();

            if (!productosFamilia.Any()) return;

            var atributosFamilia = await _atributoRepository.GetAtributosByFamiliaIdAsync(idFamilia);
            var obligatoriosOrdenados = atributosFamilia
                .Where(fa => fa.Obligatorio)
                .OrderBy(fa => fa.IdFamiliaAtributo)
                .ToList();

            var familia = await _familiaRepository.GetByIdAsync(idFamilia);
            if (familia == null) throw new InvalidOperationException("Familia no encontrada.");

            var rubro = familia.Rubro ?? await _rubroRepository.GetByIdAsync(familia.IdRubro);
            var codigoBaseGeneral = $"{rubro!.CodigoRubro}-{familia.CodigoFamilia}";

            foreach (var prod in productosFamilia)
            {
                var valoresAttrs = await _productoRepository.GetAtributosValorByProductoAsync(prod.IdProducto);
                var skuAttributesValues = new List<string>();

                foreach (var fa in obligatoriosOrdenados)
                {
                    var inputAttr = valoresAttrs.FirstOrDefault(v => v.IdAtributo == fa.IdAtributo);
                    if (inputAttr == null) continue;

                    string attrVal = "";

                    if (!string.IsNullOrWhiteSpace(inputAttr.ValorTexto))
                        attrVal = inputAttr.ValorTexto.Trim();
                    else if (inputAttr.ValorNumero.HasValue)
                        attrVal = inputAttr.ValorNumero.Value.ToString();
                    else if (inputAttr.ValorDecimal.HasValue)
                        attrVal = inputAttr.ValorDecimal.Value.ToString("0.##");
                    else if (!string.IsNullOrWhiteSpace(inputAttr.ValorLista))
                        attrVal = inputAttr.ValorLista.Trim();
                    else if (inputAttr.ValorBool.HasValue)
                        attrVal = inputAttr.ValorBool.Value ? "SI" : "NO";

                    if (!string.IsNullOrEmpty(attrVal))
                    {
                        skuAttributesValues.Add(attrVal.Replace(" ", ""));
                    }
                }

                var codigoBaseActualizado = codigoBaseGeneral;
                if (skuAttributesValues.Any())
                {
                    codigoBaseActualizado += "-" + string.Join("-", skuAttributesValues);
                }
                codigoBaseActualizado += "-";

                var parts = prod.Sku.Split('-');
                var correlativo = parts.Last();

                prod.Sku = $"{codigoBaseActualizado}{correlativo}";
                
                // Realizamos el UPDATE
                // Recordar desactivar ChangeTracking global si se hiciera foreach grande, pero como es en memoria no es tan lento el Update (ya que EF lo rastrea)
                await _productoRepository.UpdateProductoAsync(prod);
            }
        }

        private ProductoDto MapToDto(Producto p)
        {
            return new ProductoDto
            {
                IdProducto = p.IdProducto,
                IdFamilia = p.IdFamilia,
                NombreFamilia = p.Familia?.Nombre ?? "",
                Sku = p.Sku,
                Nombre = p.Nombre,
                UnidadMedida = p.UnidadMedida,
                Activo = p.Activo,
                PuntoReposicion = p.Stocks.FirstOrDefault()?.PuntoReposicion ?? 0,
                FechaCreacion = p.FechaCreacion,
                FechaActualizacion = p.FechaActualizacion,
                Atributos = p.ProductoAtributoValores.Select(pav => new ProductoAtributoValorDto
                {
                    IdAtributo = pav.IdAtributo,
                    CodigoAtributo = pav.Atributo?.CodigoAtributo ?? "",
                    NombreAtributo = pav.Atributo?.Nombre ?? "",
                    TipoDatoAtributo = pav.Atributo?.TipoDato ?? "",
                    ValorTexto = pav.ValorTexto,
                    ValorNumero = pav.ValorNumero,
                    ValorDecimal = pav.ValorDecimal,
                    ValorBool = pav.ValorBool,
                    ValorLista = pav.ValorLista
                }).ToList()
            };
        }
    }
}
