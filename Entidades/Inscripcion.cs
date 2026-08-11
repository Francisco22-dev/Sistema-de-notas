using System;

namespace Entidades
{
    /// <summary>Tabla INSCRIPCION: matricula de un estudiante en un periodo academico.</summary>
    public class Inscripcion
    {
        public int Id { get; set; }
        public int PeriodoId { get; set; }
        public int EstudianteId { get; set; }
        public int GradoSeccionId { get; set; }

        // ENUM('Nuevo Ingreso','Regular','Repitiente')
        public string TipoIngreso { get; set; } = "Nuevo Ingreso";
        public string? ColegioProcedencia { get; set; }
        // ENUM('Media General','Media Tecnica')
        public string NivelAcademico { get; set; } = "Media General";
        public DateTime FechaInscripcion { get; set; } = DateTime.Now;
    }
}
