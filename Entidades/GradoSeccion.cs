namespace Entidades
{
    /// <summary>Tabla GRADO_SECCION: combinacion de un grado con una seccion.</summary>
    public class GradoSeccion
    {
        public int Id { get; set; }
        public int GradoId { get; set; }
        public int SeccionId { get; set; }
        public string GradoNombre { get; set; } = string.Empty;
        public string SeccionNombre { get; set; } = string.Empty;

        public override string ToString() => $"{GradoNombre} \"{SeccionNombre}\"";
    }
}
