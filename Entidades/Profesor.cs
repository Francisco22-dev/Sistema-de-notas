using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entidades
{
    /// <summary>Tabla PROFESOR (los datos personales viven en PERSONA).</summary>
    public class Profesor
    {
        public int Id { get; set; }
        // ENUM('Secundaria')
        public string TipoNivel { get; set; } = "Secundaria";
        public int PersonaId { get; set; }
        // ENUM('Activo','Inactivo')
        public string Estado { get; set; } = "Activo";

        public Persona Persona { get; set; } = new Persona();
    }
}
