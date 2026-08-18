using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entidades
{
    /// <summary>Tabla MATERIA_PROFESOR: Materias que un profesor está capacitado para impartir.</summary>
    public class MateriaProfesor
    {
        public int Id { get; set; }
        public int? MateriaId { get; set; }
        public int ProfesorId { get; set; }

        public string MateriaNombre { get; set; } = string.Empty;
        public string ProfesorNombre { get; set; } = string.Empty;
    }
}