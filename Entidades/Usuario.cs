namespace Entidades
{
    /// <summary>Tabla USUARIO.</summary>
    public class Usuario
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        // ENUM('Administrador','Secretaria','Docente')
        public string Rol { get; set; } = "Secretaria";
        public string Estado { get; set; } = "Activo";   // ENUM('Activo','Inactivo')
    }
}
