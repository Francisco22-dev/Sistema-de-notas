namespace Entidades
{
    public class PlanEvaluacionMpp
    {
        public int Id { get; set; }
        public int MateriaProfesorPeriodoId { get; set; }
        public string Lapso { get; set; } = "1er lapso";
        public int NroEvaluacion { get; set; }
        public string NombreActividad { get; set; } = string.Empty;
        public decimal Porcentaje { get; set; } = 20;
    }
}