namespace Entidades
{
    /// <summary>Tabla PERSONA_ESTUDIANTE (los datos personales viven en PERSONA).</summary>
    public class Estudiante
    {
        public int Id { get; set; }
        public string CedulaEscolar { get; set; } = string.Empty;
        public int NumeroHijo { get; set; } = 1;
        // ENUM('Derecha','Izquierda','Ambidextro')
        public string Lateralidad { get; set; } = "Derecha";

        public int PersonaId { get; set; }
        public int PaisNacimientoId { get; set; } = Pais.VenezuelaId;
        /// <summary>Obligatoria si nacio en Venezuela; debe ir NULL en cualquier otro pais (CHECK check_lugar_nacimiento).</summary>
        public int? ParroquiaNacimientoId { get; set; }

        public int AntropometricoId { get; set; }
        public int SaludId { get; set; }
        public int ExtraCurricularId { get; set; }

        public int RepresentantePrincipalId { get; set; }
        public int? RepresentanteSecundarioId { get; set; }

        public string Estado { get; set; } = "Activo";   // ENUM('Activo','Retirado','Egresado')

        public Persona Persona { get; set; } = new Persona();
        public Antropometricos Antropometricos { get; set; } = new Antropometricos();
        public Salud Salud { get; set; } = new Salud();
        public ExtraCurricular ExtraCurricular { get; set; } = new ExtraCurricular();
    }
}
