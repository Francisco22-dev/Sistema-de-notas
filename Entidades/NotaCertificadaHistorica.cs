using System;
using System.Collections.Generic;

namespace Entidades
{
    public class NotaCertificadaHistorica
    {
        public int Id { get; set; }
        public string Cedula { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public DateTime? FechaNacimiento { get; set; }
        public string PaisNacimiento { get; set; } = "VENEZUELA";
        public string EstadoNacimiento { get; set; } = "CARABOBO";
        public string MunicipioNacimiento { get; set; } = "VALENCIA";
        public string PlantelEgreso { get; set; } = "UNIDAD EDUCATIVA CARABOBO";
        public decimal PromedioGeneral { get; set; }
        public string? Observaciones { get; set; }

        public CertificacionEstudianteCompletaDto DatosPensum { get; set; } = new();
    }
}