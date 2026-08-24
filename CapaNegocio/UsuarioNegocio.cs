using System;
using System.Data;
using Entidades;
using SistemaLiceo.Datos;

namespace SistemaLiceo.Negocio
{
    public class UsuarioNegocio
    {
        private readonly UsuarioDatos _datos = new UsuarioDatos();

        public Usuario? Autenticar(string nombre, string clave)
        {
            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(clave))
                return null;

            return _datos.Autenticar(nombre.Trim(), Seguridad.CifrarClave(clave));
        }

        public Usuario? BuscarPorId(int id) => _datos.BuscarPorId(id);

        public DataTable ListarTodos() => _datos.ListarTodos();

        public int Registrar(Usuario usuario, string clave)
        {
            if (string.IsNullOrWhiteSpace(usuario.Nombre))
                throw new ArgumentException("El nombre de usuario es obligatorio.");
            if (string.IsNullOrWhiteSpace(clave) || clave.Length < 6)
                throw new ArgumentException("La contraseña debe tener al menos 6 caracteres.");

            if (_datos.ExisteNombreUsuario(usuario.Nombre))
                throw new Exception($"El nombre de usuario '{usuario.Nombre}' ya está en uso.");

            return _datos.Registrar(usuario, Seguridad.CifrarClave(clave));
        }

        public void Actualizar(Usuario usuario, string? nuevaClave = null)
        {
            if (string.IsNullOrWhiteSpace(usuario.Nombre))
                throw new ArgumentException("El nombre de usuario es obligatorio.");

            if (_datos.ExisteNombreUsuario(usuario.Nombre, usuario.Id))
                throw new Exception($"El nombre de usuario '{usuario.Nombre}' ya pertenece a otro usuario.");

            string? claveCifrada = null;
            if (!string.IsNullOrWhiteSpace(nuevaClave))
            {
                if (nuevaClave.Length < 6)
                    throw new ArgumentException("La nueva contraseña debe tener al menos 6 caracteres.");
                claveCifrada = Seguridad.CifrarClave(nuevaClave);
            }

            _datos.Actualizar(usuario, claveCifrada);
        }

        public void AlternarEstado(int id, string estadoActual)
        {
            string nuevoEstado = estadoActual == "Activo" ? "Inactivo" : "Activo";
            _datos.CambiarEstado(id, nuevoEstado);
        }
    }
}