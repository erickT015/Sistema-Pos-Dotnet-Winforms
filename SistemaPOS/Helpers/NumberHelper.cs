using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace SistemaPOS.Helpers
{
    public static class NumberHelper
    {

        //FUNCION PARA PARSEAR A DECIMAL
        public static bool TryParseDecimal(object value, out decimal resultado)
        {
            resultado = 0;

            if (value == null) return false;

            var texto = value.ToString()?.Trim();

            if (string.IsNullOrWhiteSpace(texto)) return false;

            // 🔥 Permite ambos formatos: 1500.50 y 1500,50
            texto = texto.Replace(".", ",");

            return decimal.TryParse(
                texto,
                NumberStyles.Any,
                CultureInfo.CurrentCulture,
                out resultado
            );
        }

        //FUNCION PARA PARSEAR A DECIMAL
        public static bool TryParseInt(object input, out int result)
        {
            result = 0;

            if (input == null) return false;

            var str = input.ToString()?.Trim();

            return int.TryParse(str, out result);
        }
    }
}
