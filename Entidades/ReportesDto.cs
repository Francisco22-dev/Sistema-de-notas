using System;
using System.Collections.Generic;

namespace Entidades
{
    public class ConstanciaEstudioDto
    {
        public string EstudianteNombreCompleto { get; set; } = string.Empty;
        public string Cedula { get; set; } = string.Empty;
        public string CedulaEscolar { get; set; } = string.Empty;
        public string Grado { get; set; } = string.Empty;
        public string Seccion { get; set; } = string.Empty;
        public string Periodo { get; set; } = string.Empty;
        public string NivelAcademico { get; set; } = string.Empty;
        public DateTime FechaInscripcion { get; set; }
    }

    public class FilaBoletaDto
    {
        public string Materia { get; set; } = string.Empty;
        public string Docente { get; set; } = string.Empty;
        public int? NotaLapso1 { get; set; }
        public int? NotaLapso2 { get; set; }
        public int? NotaLapso3 { get; set; }
        public int? NotaDefinitiva { get; set; }
    }

    public class FilaNominaSeccionDto
    {
        public int Numero { get; set; }
        public string Cedula { get; set; } = string.Empty;
        public string CedulaEscolar { get; set; } = string.Empty;
        public string Estudiante { get; set; } = string.Empty;
        public string Sexo { get; set; } = string.Empty;
        public string Representante { get; set; } = string.Empty;
        public string TelefonoRepresentante { get; set; } = string.Empty;
    }
    public class FilaSazeMatriculaDto
    {
        public int Numero { get; set; }
        public string Cedula { get; set; } = string.Empty;
        public string CedulaEscolar { get; set; } = string.Empty;
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string Sexo { get; set; } = string.Empty;
        public DateTime? FechaNacimiento { get; set; }
        public int Edad { get; set; }
        public string LugarNacimiento { get; set; } = string.Empty;
        public string TipoIngreso { get; set; } = string.Empty;
    }

    public class FilaSazeRendimientoDto
    {
        public string Materia { get; set; } = string.Empty;
        public string Docente { get; set; } = string.Empty;
        public int Inscritos { get; set; }
        public int Evaluados { get; set; }
        public int Aprobados { get; set; }
        public int Aplazados { get; set; }
        public decimal PorcentajeAprobados => Evaluados > 0 ? Math.Round((decimal)Aprobados / Evaluados * 100, 1) : 0;
    }
    public class FilaNotaCertificadaDto
    {
        public string Grado { get; set; } = string.Empty;
        public string Materia { get; set; } = string.Empty;
        public string Periodo { get; set; } = string.Empty;
        public int? NotaNumero { get; set; }
        public string NotaLetras { get; set; } = string.Empty;
    }
}