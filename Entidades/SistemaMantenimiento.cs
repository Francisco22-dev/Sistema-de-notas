using System;

namespace Entidades
{
    public class SistemaMantenimiento
    {
        public int Id { get; set; } = 1;
        public bool BloqueoManual { get; set; } = false;
        public DateTime FechaLimite { get; set; } = DateTime.Now.AddMonths(6);
        public int FrecuenciaMeses { get; set; } = 6;
        public string Mensaje { get; set; } = "Sistema en mantenimiento preventivo programado. Por favor, comuníquese con el servicio de soporte técnico para la revisión periódica.";
    }
}