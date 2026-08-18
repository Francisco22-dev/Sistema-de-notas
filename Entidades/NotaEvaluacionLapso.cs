using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entidades
{
    /// <summary>Tabla NOTA_EVALUACION_LAPSO: Calificación ponderada dentro de un lapso.</summary>
    public class NotaEvaluacionLapso
    {
        public int Id { get; set; }
        public int NotaLapsoId { get; set; }
        public int EvaluacionId { get; set; }
        public int Porcentaje { get; set; }

        public Evaluacion Evaluacion { get; set; } = new Evaluacion();
    }
}