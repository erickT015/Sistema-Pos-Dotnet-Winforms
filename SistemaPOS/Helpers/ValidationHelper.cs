using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SistemaPOS.Helpers
{
    public static class ValidationHelper
    {
        public static bool EsValido(object modelo)
        {
            var contexto = new ValidationContext(modelo);
            var resultados = new List<ValidationResult>();

            bool esValido = Validator.TryValidateObject(modelo, contexto, resultados, true);

            if (!esValido)
            {
                string errores = string.Join("\n", resultados.Select(r => r.ErrorMessage));

                MessageBox.Show(
                    errores,
                    "Validación",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }

            return esValido;
        }
    }
}
