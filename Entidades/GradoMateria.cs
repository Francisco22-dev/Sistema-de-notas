using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entidades
{
    /// <summary>Tabla GRADO_MATERIA: Asignación de una materia al pensum de un año/grado.</summary>
    public class GradoMateria
    {
        public int Id { get; set; }
        public int GradoId { get; set; }
        public int? MateriaId { get; set; }

        public string GradoNombre { get; set; } = string.Empty;
        public string MateriaNombre { get; set; } = string.Empty;
    }
}
