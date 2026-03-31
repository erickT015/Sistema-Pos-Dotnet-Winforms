using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace SistemaPOS.Models
{
    public class Producto
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Codigo { get; set; }

        [Required]
        [MaxLength(150)]
        public string Nombre { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal PrecioCompra { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal PrecioVenta { get; set; }

        [Required]
        public int Cantidad { get; set; }
        public int CantidadMinima { get; set; }
        public int? TipoVentaId { get; set; }
        public int? CategoriaId { get; set; }
        public int? ProveedorId { get; set; }

        public bool Activo { get; set; } = true;
    }
}
