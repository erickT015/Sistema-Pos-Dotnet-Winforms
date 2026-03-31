using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace SistemaPOS.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [MaxLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
        [MinLength(3, ErrorMessage = "El nombre debe tener al menos 3 caracteres.")]
        public string Nombre { get; set; }

        public string Password { get; set; }

        [NotMapped]
        [MinLength(6, ErrorMessage = "La nueva contraseña debe tener al menos 6 caracteres.")]
        [Display(Name = "Nueva Contraseña")]
        public string CambiarPassword { get; set; }

        [Required(ErrorMessage = "Debe asignar un rol")]
        [RegularExpression("^(ADMIN|CAJERO)$", ErrorMessage = "El rol debe ser 'ADMIN' o 'CAJERO'.")]
        public string Rol { get; set; }

        public bool Activo { get; set; } = true;
    }
}
