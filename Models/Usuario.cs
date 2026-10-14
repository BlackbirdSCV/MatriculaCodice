using System;
using System.Security.Cryptography;

namespace SistemaCodice.Models
{
    /// <summary>Clase base de Estudiante y Profesor (diagrama UML: Usuario).</summary>
    public abstract class Usuario
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; } = "";
        public string Rut { get; set; } = "";
        public string Correo { get; set; } = "";

        /// <summary>RNF02: la contraseña nunca se guarda en texto plano (PBKDF2-SHA256 con sal).</summary>
        public string ContrasenaHash { get; set; } = "";

        public static string Hash(string texto)
        {
            byte[] sal = RandomNumberGenerator.GetBytes(16);
            byte[] h = Rfc2898DeriveBytes.Pbkdf2(texto ?? "", sal, 100000, HashAlgorithmName.SHA256, 32);
            return Convert.ToBase64String(sal) + ":" + Convert.ToBase64String(h);
        }

        public static bool Verificar(string texto, string almacenado)
        {
            try
            {
                string[] partes = (almacenado ?? "").Split(':');
                if (partes.Length != 2) return false;
                byte[] sal = Convert.FromBase64String(partes[0]);
                byte[] esperado = Convert.FromBase64String(partes[1]);
                byte[] h = Rfc2898DeriveBytes.Pbkdf2(texto ?? "", sal, 100000, HashAlgorithmName.SHA256, 32);
                return CryptographicOperations.FixedTimeEquals(h, esperado);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public bool IniciarSesion(string correo, string contrasena)
        {
            return string.Equals(Correo, correo, StringComparison.OrdinalIgnoreCase)
                   && Verificar(contrasena, ContrasenaHash);
        }

        public void CerrarSesion()
        {
            // La sesión vive en la interfaz (MainForm); aquí no hay estado que liberar.
        }
    }
}
