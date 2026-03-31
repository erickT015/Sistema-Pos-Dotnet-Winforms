using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SistemaPOS.Models
{
    public class Proveedor
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [MaxLength(150, ErrorMessage = "El nombre no puede pasar de 150 caracteres.")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El teléfono es necesario para el proveedor.")]
        [MaxLength(50)]
        public string Telefono { get; set; }

        [MaxLength(150)]
        [EmailAddress(ErrorMessage = "Ese correo electrónico no parece válido.")]
        public string Email { get; set; }

        public bool Activo { get; set; } = true;
    }
}
