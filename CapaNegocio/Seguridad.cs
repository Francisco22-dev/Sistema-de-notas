using System;
using System.Security.Cryptography;
using System.Text;

namespace SistemaLiceo.Negocio
{
    /// <summary>Utilidades de seguridad para las contrasenas de la tabla USUARIO.</summary>
    public static class Seguridad
    {
        /// <summary>Cifra una contrasena en SHA-256 (64 caracteres hexadecimales en minusculas).</summary>
        public static string CifrarClave(string clave)
        {
            byte[] resumen = SHA256.HashData(Encoding.UTF8.GetBytes(clave));
            return Convert.ToHexString(resumen).ToLowerInvariant();
        }
    }
}
