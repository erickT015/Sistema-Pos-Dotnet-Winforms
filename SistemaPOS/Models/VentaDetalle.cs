using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace SistemaPOS.Models
{
    public class VentaDetalle
    {
  
        public int Id { get; set; }

        [Required]
        public int VentaId { get; set; }

        [Required]
        public int ProductoId { get; set; }

        [Range(1, int.MaxValue)]
        public int Cantidad { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal PrecioUnitario { get; set; }

        [NotMapped]
        public decimal Subtotal => Cantidad * PrecioUnitario;
    }
}
