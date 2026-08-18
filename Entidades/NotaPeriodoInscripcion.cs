using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;

namespace Entidades
{
    /// <summary>Tabla NOTA_PERIODO_INSCRIPCION: Definitiva anual de la materia para un alumno inscrito.</summary>
    public class NotaPeriodoInscripcion
    {
        public int Id { get; set; }
        public int InscripcionId { get; set; }
        public int MateriaProfePeriodoId { get; set; }
        public int? Nota { get; set; }
        public DateTime? CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }
    }
}