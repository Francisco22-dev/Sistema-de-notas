using System;
using Entidades;
using SistemaLiceo.Datos;

namespace SistemaLiceo.Negocio
{
    /// <summary>Reglas de acceso al sistema.</summary>
    public class UsuarioNegocio
    {
        private readonly UsuarioDatos _datos = new UsuarioDatos();

        /// <summary>Valida las credenciales y devuelve el usuario, o null si no son correctas.</summary>
        public Usuario? Autenticar(string nombre, string clave)
        {
            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(clave))
                return null;

            return _datos.Autenticar(nombre.Trim(), Seguridad.CifrarClave(clave));
        }

        public int Registrar(Usuario usuario, string clave)
        {
            if (string.IsNullOrWhiteSpace(usuario.Nombre))
                throw new ArgumentException("El nombre de usuario es obligatorio.");
            if (clave.Length < 6)
                throw new ArgumentException("La contrasena debe tener al menos 6 caracteres.");

            return _datos.Registrar(usuario, Seguridad.CifrarClave(clave));
        }
    }
}
