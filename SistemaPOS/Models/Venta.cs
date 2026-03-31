using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SistemaPOS.Models
{
    public class Venta
    {
        public int Id { get; set; }

        [Required]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required]
        public int UsuarioId { get; set; }

        [Required]
        public decimal Total { get; set; }
    }
}
