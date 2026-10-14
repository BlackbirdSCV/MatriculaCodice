using System.Linq;
using System.Text;

namespace SistemaCodice.Models
{
    /// <summary>Validaciones y formato de datos comunes (RUT chileno, correo).</summary>
    public static class Validador
    {
        public static string LimpiarRut(string rut)
        {
            return (rut ?? "").Replace(".", "").Replace("-", "").Replace(" ", "").ToUpperInvariant();
        }

        /// <summary>Parte numérica del RUT (sin puntos, guion ni dígito verificador).</summary>
        public static string CuerpoRut(string rut)
        {
            string limpio = LimpiarRut(rut);
            return limpio.Length > 1 ? limpio.Substring(0, limpio.Length - 1) : "";
        }

        /// <summary>Valida el dígito verificador con el algoritmo módulo 11.</summary>
        public static bool RutValido(string rut)
        {
            string limpio = LimpiarRut(rut);
            if (limpio.Length < 2) return false;

            string cuerpo = limpio.Substring(0, limpio.Length - 1);
            char dv = limpio[limpio.Length - 1];
            if (!cuerpo.All(char.IsDigit)) return false;

            int suma = 0, mult = 2;
            for (int i = cuerpo.Length - 1; i >= 0; i--)
            {
                suma += (cuerpo[i] - '0') * mult;
                mult = mult == 7 ? 2 : mult + 1;
            }
            int resto = 11 - (suma % 11);
            char esperado = resto == 11 ? '0' : resto == 10 ? 'K' : (char)('0' + resto);
            return dv == esperado;
        }

        /// <summary>Devuelve el RUT con formato 12.345.678-5.</summary>
        public static string FormatearRut(string rut)
        {
            string limpio = LimpiarRut(rut);
            if (limpio.Length < 2) return rut;
            string cuerpo = limpio.Substring(0, limpio.Length - 1);
            char dv = limpio[limpio.Length - 1];

            var sb = new StringBuilder();
            int contador = 0;
            for (int i = cuerpo.Length - 1; i >= 0; i--)
            {
                if (contador > 0 && contador % 3 == 0) sb.Insert(0, '.');
                sb.Insert(0, cuerpo[i]);
                contador++;
            }
            return sb.ToString() + "-" + dv;
        }

        public static bool CorreoValido(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo)) return false;
            int arroba = correo.IndexOf('@');
            return arroba > 0 && arroba == correo.LastIndexOf('@')
                   && correo.IndexOf('.', arroba) > arroba + 1
                   && !correo.EndsWith(".") && !correo.Contains(" ");
        }
    }
}
