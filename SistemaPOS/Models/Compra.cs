using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace SistemaPOS.Models
{
    public class Compra
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;

        public int? ProveedorId { get; set; }

        [Required]
        public int UsuarioId { get; set; }


        [Column(TypeName = "decimal(10,2)")]
        public decimal Total { get; set; }
    }
}
