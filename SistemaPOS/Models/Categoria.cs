using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SistemaPOS.Models
{
    public class Categoria
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MaxLength(100, ErrorMessage = "El nombre no puede pasar de 100 caracteres.")]
        public string Nombre { get; set; }

        public bool Activo { get; set; } = true;
    }
}
