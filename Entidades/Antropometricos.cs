namespace Entidades
{
    /// <summary>Tabla ANTROPOMETRICOS.</summary>
    public class Antropometricos
    {
        public int Id { get; set; }
        public decimal? Estatura { get; set; }     // DECIMAL(4,2) en metros
        public decimal? Peso { get; set; }         // DECIMAL(5,2) en kilogramos
        public string? TallaCamisa { get; set; }
        public string? TallaPantalon { get; set; }
        public int? TallaZapato { get; set; }
    }
}
