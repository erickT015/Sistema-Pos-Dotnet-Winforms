using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace SistemaPOS.Models
{
    public class Producto
    {
        public int Id { get; set; } // Primary key

        [Required]
        [MaxLength(50)]
        public string Codigo { get; set; } // CODIGO UNICO PARA CADA PRODUCTO

        [Required]
        [MaxLength(150)]
        public string Nombre { get; set; } // NOMBRE DEL PRODUCTO

        [Column(TypeName = "decimal(10,2)")]
        public decimal PrecioCompra { get; set; } // PRECIO DE COMPRA DEL PRODUCTO

        [Column(TypeName = "decimal(10,2)")]
        public decimal PrecioVenta { get; set; } // PRECIO DE VENTA DEL PRODUCTO

        [Required]
        public int Cantidad { get; set; } // CANTIDAD EN STOCK DEL PRODUCTO
        public int CantidadMinima { get; set; } // CANTIDAD MINIMA EN STOCK PARA ALERTAS
        public int? TipoVentaId { get; set; } // ID DE LA FORMA DE VENTA (POR UNIDAD, POR PESO, ETC.)
        public int? CategoriaId { get; set; } // ID DE LA CATEGORIA A LA QUE PERTENECE EL PRODUCTO
        public int? ProveedorId { get; set; } // ID DEL PROVEEDOR DEL PRODUCTO

        [Column(TypeName = "decimal(10,2)")]
        public decimal? GananciaUnitaria { get; set; } // GANANCIA POR UNIDAD VENDIDA

        [Column(TypeName = "decimal(5,2)")]
        public decimal? PorcentajeGanancia { get; set; } // PORCENTAJE DE GANANCIA SOBRE EL PRECIO DE COMPRA

        public bool Activo { get; set; } = true; // INDICA SI EL PRODUCTO ESTÁ ACTIVO O INACTIVO (ELIMINACIÓN LÓGICA)
    }
}
