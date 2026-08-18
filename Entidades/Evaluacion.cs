using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entidades
{
    /// <summary>Tabla EVALUACION.</summary>
    public class Evaluacion
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public int Nota { get; set; }
    }
}
