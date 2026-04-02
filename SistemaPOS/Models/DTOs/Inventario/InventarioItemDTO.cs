using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SistemaPOS.Models.DTOs.Inventario
{
    public class InventarioItemDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El es Código requerido")]
        [MaxLength(50)]
        public string Codigo { get; set; } // CODIGO UNICO PARA CADA PRODUCTO


        [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener al menos 3 caracteres.")] 
        public string Nombre { get; set; } // NOMBRE DEL PRODUCTO


        [Range(0, double.MaxValue, ErrorMessage = "Precio compra inválido")]
        public decimal PrecioCompra { get; set; } // PRECIO DE COMPRA DEL PRODUCTO


        [Required(ErrorMessage = "El precio de venta es obligatorio.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio de venta debe ser mayor a 0.")] 
        public decimal PrecioVenta { get; set; } // PRECIO DE VENTA DEL PRODUCTO

        public int StockActual { get; set; } // CANTIDAD EN STOCK DEL PRODUCTO

        public int Ajuste { get; set; } = 0; // CANTIDAD DE AJUSTE (POSITIVO O NEGATIVO) PARA CORREGIR EL STOCK

        public int Total => StockActual + Ajuste; // CANTIDAD TOTAL DESPUÉS DE APLICAR EL AJUSTE

        public int CantidadMinima { get; set; } // CANTIDAD MINIMA EN STOCK PARA ALERTAS

        public int? TipoVentaId { get; set; } // ID DE LA FORMA DE VENTA (POR UNIDAD, POR PESO, ETC.)
        public int? CategoriaId { get; set; } // ID DE LA CATEGORIA A LA QUE PERTENECE EL PRODUCTO
        public int? ProveedorId { get; set; } // ID DEL PROVEEDOR DEL PRODUCTO

        public string CategoriaNombre { get; set; } // NOMBRE DE LA CATEGORIA (PARA MOSTRAR EN LA UI)
        public string ProveedorNombre { get; set; } // NOMBRE DEL PROVEEDOR (PARA MOSTRAR EN LA UI)

        public decimal GananciaUnitaria { get; set; } // GANANCIA POR UNIDAD VENDIDA
        public decimal PorcentajeGanancia { get; set; } // PORCENTAJE DE GANANCIA SOBRE EL PRECIO DE COMPRA
        public bool Activo { get; set; } = true; // INDICA SI EL PRODUCTO ESTÁ ACTIVO O INACTIVO (ELIMINACIÓN LÓGICA)

        public bool EsNuevo { get; set; } = false; // INDICA SI EL ITEM ES NUEVO (AGREGADO RECIENTEMENTE EN LA UI)

        public bool IsSelected { get; set; } = false; // PROPIEDAD PARA CONTROLAR LA SELECCIÓN DEL ITEM EN LA UI 



        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (PrecioVenta <= PrecioCompra && PrecioCompra > 0)
            {
                yield return new ValidationResult(
                    "El precio de venta debe ser mayor al precio de compra.",
                    new[] { nameof(PrecioVenta) });
            }


            if (StockActual > 0 && PrecioCompra <= 0)
            {
                yield return new ValidationResult(
                    "No puede tener stock con precio de compra en 0.",
                    new[] { nameof(PrecioCompra) });
            }

            if (GananciaUnitaria < 0)
            {
                yield return new ValidationResult(
                    "La ganancia no puede ser negativa.",
                    new[] { nameof(GananciaUnitaria) });
            }

            if (CategoriaId == null || CategoriaId <= 0)
            {
                yield return new ValidationResult(
                    "Debe seleccionar una categoría.",
                    new[] { nameof(CategoriaId) });
            }
        }
    }

}
