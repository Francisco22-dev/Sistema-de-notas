using System;

namespace Entidades
{
    /// <summary>Tabla PERSONA: datos comunes de estudiantes, representantes y profesores.</summary>
    public class Persona
    {
        public int Id { get; set; }
        public string Nacionalidad { get; set; } = "V";   // ENUM('V','E')
        public string? CedulaIdentidad { get; set; }      // UNIQUE, admite NULL en menores sin cedula
        public string Nombre1 { get; set; } = string.Empty;
        public string? Nombre2 { get; set; }
        public string Apellido1 { get; set; } = string.Empty;
        public string? Apellido2 { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string Sexo { get; set; } = "M";           // ENUM('F','M')
        public int? DireccionId { get; set; }

        public Direccion? Direccion { get; set; }

        public string NombreCompleto =>
            $"{Nombre1} {Nombre2} {Apellido1} {Apellido2}".Replace("  ", " ").Trim();

        /// <summary>Cedula con el prefijo de nacionalidad, por ejemplo V-9857670.</summary>
        public string CedulaFormateada =>
            string.IsNullOrWhiteSpace(CedulaIdentidad) ? string.Empty : $"{Nacionalidad}-{CedulaIdentidad}";
    }
}
