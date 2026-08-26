using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace SistemaLiceo.Negocio
{
    public static class Seguridad
    {
        // Control de bloqueo por fuerza bruta en memoria
        private static readonly ConcurrentDictionary<string, (int Intentos, DateTime UltimoFallo)> IntentosFallidos = new();
        private const int MaxIntentos = 4;
        private static readonly TimeSpan TiempoBloqueo = TimeSpan.FromMinutes(3);

        public static string CifrarClave(string clave)
        {
            byte[] resumen = SHA256.HashData(Encoding.UTF8.GetBytes(clave));
            return Convert.ToHexString(resumen).ToLowerInvariant();
        }

        public static bool EstaBloqueado(string usuario, out int minutosRestantes)
        {
            minutosRestantes = 0;
            if (IntentosFallidos.TryGetValue(usuario.ToLowerInvariant(), out var info))
            {
                if (info.Intentos >= MaxIntentos)
                {
                    TimeSpan transcurrido = DateTime.Now - info.UltimoFallo;
                    if (transcurrido < TiempoBloqueo)
                    {
                        minutosRestantes = (int)Math.Ceiling((TiempoBloqueo - transcurrido).TotalMinutes);
                        return true;
                    }
                    // Si ya pasó el tiempo, reseteamos
                    IntentosFallidos.TryRemove(usuario.ToLowerInvariant(), out _);
                }
            }
            return false;
        }

        public static void RegistrarIntentoFallido(string usuario)
        {
            string clave = usuario.ToLowerInvariant();
            IntentosFallidos.AddOrUpdate(clave,
                (1, DateTime.Now),
                (_, anterior) => (anterior.Intentos + 1, DateTime.Now));
        }

        public static void LimpiarIntentos(string usuario)
        {
            IntentosFallidos.TryRemove(usuario.ToLowerInvariant(), out _);
        }
    }
}