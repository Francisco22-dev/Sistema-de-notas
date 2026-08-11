namespace Entidades
{
    /// <summary>Fila generica id/nombre usada por los catalogos (PAIS, ESTADO, CIUDAD, GRADO, ...).</summary>
    public class Catalogo
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;

        public Catalogo() { }

        public Catalogo(int id, string nombre)
        {
            Id = id;
            Nombre = nombre;
        }

        public override string ToString() => Nombre;
    }

    public static class Pais
    {
        /// <summary>Id de Venezuela en la tabla PAIS; lo exige el CHECK de PERSONA_ESTUDIANTE.</summary>
        public const int VenezuelaId = 232;
    }
}
