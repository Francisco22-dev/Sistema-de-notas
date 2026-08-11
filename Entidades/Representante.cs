namespace Entidades
{
    /// <summary>Tabla PERSONA_REPRESENTANTE (los datos personales viven en PERSONA).</summary>
    public class Representante
    {
        public int Id { get; set; }                 // 0 si aun no existe en la base de datos
        public string Parentesco { get; set; } = string.Empty;
        // ENUM('Soltera/o','Casada/o','Divorciada/o','Viuda/o','Concubinato')
        public string EstadoCivil { get; set; } = "Soltera/o";
        public decimal? IngresoMensual { get; set; }
        public string? TelefonoMovil { get; set; }
        public string? TelefonoHabitacion { get; set; }
        public string? CorreoElectronico { get; set; }
        public string? Profesion { get; set; }
        public string? EmpresaTrabajo { get; set; }
        public string? TelefonoEmpresa { get; set; }
        public string? DireccionEmpresa { get; set; }

        public int PersonaId { get; set; }
        public string Estado { get; set; } = "Activo";   // ENUM('Activo','Inactivo')

        public Persona Persona { get; set; } = new Persona();
    }
}
