using System;

namespace Entidades
{
    /// <summary>Tabla SALUD.</summary>
    public class Salud
    {
        public int Id { get; set; }
        public string ReaccionesAlergicas { get; set; } = "No";      // ENUM('Si','No')
        public string? CualesAlergias { get; set; }
        public string? EnfermedadesPadecidas { get; set; }
        public string AtencionEspecial { get; set; } = "No";         // ENUM('Si','No')
        public string? HorarioTratamiento { get; set; }
        public string AtendidoPorEspecialista { get; set; } = "No";  // ENUM('Si','No')
        public string? NombreEspecialista { get; set; }
        public DateTime? FechaInicioEspecialista { get; set; }
        public string? CondicionAtencion { get; set; }
    }
}
