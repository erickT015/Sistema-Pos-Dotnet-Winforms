using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SistemaPOS.Models.DTOs.Inventario
{
    public class InventarioItemDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El Código requerido")]
        [MaxLength(50)]
        public string Codigo { get; set; }


        [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre debe tener al menos 3 caracteres.")] 
        public string Nombre { get; set; }


        [Range(0, double.MaxValue, ErrorMessage = "Precio compra inválido")]
        public decimal PrecioCompra { get; set; }


        [Required(ErrorMessage = "El precio de venta es obligatorio.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio de venta debe ser mayor a 0.")] 
        public decimal PrecioVenta { get; set; }
        
        public int StockActual { get; set; }

        public int Ajuste { get; set; } = 0;

        public int Total => StockActual + Ajuste;

        public int CantidadMinima { get; set; }

        public int? TipoVentaId { get; set; }
        public int? CategoriaId { get; set; }
        public int? ProveedorId { get; set; }

        public string CategoriaNombre { get; set; }
        public string ProveedorNombre { get; set; }


        public bool Activo { get; set; } = true;

        // 🔥 CONTROL UI
        public bool EsNuevo { get; set; } = false;
    }
}
