using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entidades
{
    public class ConfiguracionPlantel
    {
        public int Id { get; set; } = 1;
        public string Eponimo { get; set; } = "UNIDAD EDUCATIVA CARABOBO";
        public string CodigoPlantel { get; set; } = "T0311D0814";
        public string CodigoPlanEstudio { get; set; } = "31059";
        public string DenominacionPlan { get; set; } = "EDUCACIÓN MEDIA GENERAL";
        public string Direccion { get; set; } = "PARROQUIA SAN JOSÉ, VALENCIA - ESTADO CARABOBO";
        public string Telefono { get; set; } = "0241-8217287";
        public string Municipio { get; set; } = "VALENCIA";
        public string EntidadFederal { get; set; } = "CARABOBO";
        public string Cdcee { get; set; } = "CARABOBO";
        public string DirectorNombre { get; set; } = "Felipe Fernández";
        public string DirectorCedula { get; set; } = "V-18061830";
    }
}
