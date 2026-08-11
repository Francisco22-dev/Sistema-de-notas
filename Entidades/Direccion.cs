namespace Entidades
{
    /// <summary>Tabla DIRECCION.</summary>
    public class Direccion
    {
        public int Id { get; set; }
        public int CiudadId { get; set; }
        public string? Sector { get; set; }
        public string? Avenida { get; set; }
        public string? Calle { get; set; }
        public string? Manzana { get; set; }
        public string? Vereda { get; set; }
        public string? NumeroVivienda { get; set; }

        // ENUM('Casa','Edificio','Apartamento','Quinta','Rancho','Otro')
        public string TipoVivienda { get; set; } = "Casa";
        // ENUM('Propia','Alquilada','Pagandose','Prestada','Invadida')
        public string CondicionVivienda { get; set; } = "Propia";
        // ENUM('Buena','Regular','Deteriorada')
        public string InfraestructuraVivienda { get; set; } = "Buena";
    }
}
