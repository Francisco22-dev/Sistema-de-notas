using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entidades
{
    /// <summary>Tabla MATERIA_PROFESOR_PERIODO: Carga horaria docente asignada a una sección y período.</summary>
    public class MateriaProfesorPeriodo
    {
        public int Id { get; set; }
        public int GradoSeccionId { get; set; }
        public int GradoMateriaId { get; set; }
        public int MateriaProfesorId { get; set; }
        public int PeriodoId { get; set; }

        // Propiedades de navegación descriptivas para UI
        public string Grado { get; set; } = string.Empty;
        public string Seccion { get; set; } = string.Empty;
        public string Materia { get; set; } = string.Empty;
        public string Docente { get; set; } = string.Empty;
        public string Periodo { get; set; } = string.Empty;
    }
}