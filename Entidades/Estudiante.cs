namespace Entidades
{
    public class Estudiante
    {
        public int Id { get; set; }
        public string CedulaEscolar { get; set; } = string.Empty;
        public int NumeroHijo { get; set; } = 1;
        public string Lateralidad { get; set; } = "Derecha";

        public string? TelefonoEstudiante { get; set; }
        public string? CorreoEstudiante { get; set; }

        public int PersonaId { get; set; }
        public int PaisNacimientoId { get; set; } = Pais.VenezuelaId;
        public int? ParroquiaNacimientoId { get; set; }

        public int AntropometricoId { get; set; }
        public int SaludId { get; set; }
        public int ExtraCurricularId { get; set; }

        public int RepresentantePrincipalId { get; set; }
        public int? RepresentanteSecundarioId { get; set; }

        // Datos Familiares y CPNNA
        public string SituacionPadres { get; set; } = "Viven Juntos";
        public string ConviveCon { get; set; } = "Ambos Padres";
        public string? PadreCedula { get; set; }
        public string? PadreNombresApellidos { get; set; }
        public string? PadreTelefono { get; set; }
        public string PadreVive { get; set; } = "Vivo (En el país)";
        public string? MadreCedula { get; set; }
        public string? MadreNombresApellidos { get; set; }
        public string? MadreTelefono { get; set; }
        public string MadreVive { get; set; } = "Viva (En el país)";
        public string RepresentanteLegalTipo { get; set; } = "Madre";
        public string? OficioCpnnaTribunal { get; set; }
        public string? ObservacionesCustodia { get; set; }

        // Indicadores y Discapacidad Detallada
        public string PoblacionIndigena { get; set; } = "No";
        public string Embarazada { get; set; } = "No";
        public string PoseeDiscapacidad { get; set; } = "No";
        public string? TipoDiscapacidad { get; set; }
        public string? DescripcionDiscapacidad { get; set; }
        public string? CarnetConapdis { get; set; }

        public string Estado { get; set; } = "Activo";

        public Persona Persona { get; set; } = new Persona();
        public Antropometricos Antropometricos { get; set; } = new Antropometricos();
        public Salud Salud { get; set; } = new Salud();
        public ExtraCurricular ExtraCurricular { get; set; } = new ExtraCurricular();
    }
}