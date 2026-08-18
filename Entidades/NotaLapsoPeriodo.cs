using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entidades
{
    /// <summary>Tabla NOTA_LAPSO_PERIODO: Definitiva de un lapso o reparación.</summary>
    public class NotaLapsoPeriodo
    {
        public int Id { get; set; }
        // ENUM('1er lapso','2do lapso','3er lapso','Reparacion')
        public string Nombre { get; set; } = "1er lapso";
        public int NotaPeriodoId { get; set; }
        public int Nota { get; set; }
    }
}