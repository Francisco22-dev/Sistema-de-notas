using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entidades
{
    /// <summary>DTO plano para enlazar las notas masivas de una sección al DataGrid de la UI.</summary>
    public class CalificacionEstudianteDto
    {
        public int InscripcionId { get; set; }
        public int EstudianteId { get; set; }
        public string CedulaEscolar { get; set; } = string.Empty;
        public string Cedula { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public int? EvaluacionId { get; set; }
        public int NotaEvaluacion { get; set; }
        public int? NotaDefinitivaLapso { get; set; }
    }
}