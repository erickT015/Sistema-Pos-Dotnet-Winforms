using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace SistemaPOS.Models
{
    public class Gasto
    {
        public int Id { get; set; }

        [Required]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required]
        public string Tipo { get; set; }

        [Required]
        public string Descripcion { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Monto { get; set; }

        [Required]
        public int UsuarioId { get; set; }
    }
}
