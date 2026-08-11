using System;

namespace Entidades
{
    /// <summary>Tabla EXTRA_CURRICULAR.</summary>
    public class ExtraCurricular
    {
        public int Id { get; set; }
        public string RealizaDeportes { get; set; } = "No";   // ENUM('Si','No')
        public string? CualesDeportes { get; set; }

        public string PoseeCanaima { get; set; } = "No";      // ENUM('Si','No')
        public DateTime? FechaAsignacionCanaima { get; set; }
        public string? SerialCanaima { get; set; }
        // ENUM('Operativa','Dañada','Robada','En Reparacion')
        public string? EstadoCanaima { get; set; } = "Operativa";
        public string? FallaCanaima { get; set; }

        public string PoseeCargador { get; set; } = "No";     // ENUM('Si','No')
        public string? EstadoCargador { get; set; } = "Operativo";  // ENUM('Operativo','Dañado')
        public string? FallaCargador { get; set; }
    }
}
