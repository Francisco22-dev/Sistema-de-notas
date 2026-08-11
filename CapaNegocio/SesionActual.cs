using Entidades;

namespace SistemaLiceo.Negocio
{
    /// <summary>Datos del usuario conectado, compartidos por toda la aplicacion.</summary>
    public static class SesionActual
    {
        public static int IdUsuario { get; set; }
        public static string NombreUsuario { get; set; } = string.Empty;
        public static string Rol { get; set; } = string.Empty;

        public static bool EsAdministrador => Rol == "Administrador";
        public static bool EsSecretaria => Rol == "Secretaria";
        public static bool EsDocente => Rol == "Docente";

        public static void Iniciar(Usuario usuario)
        {
            IdUsuario = usuario.Id;
            NombreUsuario = usuario.Nombre;
            Rol = usuario.Rol;
        }

        public static void LimpiarSesion()
        {
            IdUsuario = 0;
            NombreUsuario = string.Empty;
            Rol = string.Empty;
        }
    }
}
