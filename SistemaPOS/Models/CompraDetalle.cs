using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace SistemaPOS.Models
{
    public class CompraDetalle
    {
        public int Id { get; set; }

        [Required]
        public int CompraId { get; set; }

        [Required]
        public int ProductoId { get; set; }

        [Range(1, int.MaxValue)]
        public int Cantidad { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal PrecioCompra { get; set; }


        [NotMapped]
        public decimal Subtotal => Cantidad * PrecioCompra;
    }
}
