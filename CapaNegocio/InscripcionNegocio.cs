using System;
using Entidades;
using SistemaLiceo.Datos;

namespace SistemaLiceo.Negocio
{
    /// <summary>Validaciones y orquestacion del proceso de inscripcion.</summary>
    public class InscripcionNegocio
    {
        private readonly EstudianteDatos _datos = new EstudianteDatos();

        /// <summary>
        /// Valida los datos exigidos por db_carabobo y registra al estudiante con su representante
        /// y su matricula. Devuelve el id del estudiante creado.
        /// </summary>
        public int RegistrarInscripcionCompleta(Representante representante, Estudiante estudiante, Inscripcion inscripcion)
        {
            Validar(representante, estudiante, inscripcion);

            if (_datos.ExisteCedulaEscolar(estudiante.CedulaEscolar))
                throw new Exception("La cedula escolar " + estudiante.CedulaEscolar + " ya esta registrada.");

            return _datos.RegistrarInscripcionCompleta(representante, estudiante, inscripcion);
        }

        private static void Validar(Representante representante, Estudiante estudiante, Inscripcion inscripcion)
        {
            if (string.IsNullOrWhiteSpace(estudiante.CedulaEscolar))
                throw new Exception("La cedula escolar del estudiante es obligatoria.");

            if (string.IsNullOrWhiteSpace(estudiante.Persona.Nombre1) ||
                string.IsNullOrWhiteSpace(estudiante.Persona.Apellido1))
                throw new Exception("El primer nombre y el primer apellido del estudiante son obligatorios.");

            if (estudiante.Persona.FechaNacimiento == null)
                throw new Exception("Seleccione la fecha de nacimiento del estudiante.");

            // La base de datos exige la parroquia solo para quienes nacieron en Venezuela
            // (restriccion check_lugar_nacimiento de PERSONA_ESTUDIANTE).
            if (estudiante.PaisNacimientoId == Pais.VenezuelaId && estudiante.ParroquiaNacimientoId == null)
                throw new Exception("Indique la parroquia de nacimiento del estudiante.");

            // Ojo: la columna parroquia_nacimiento_id es NOT NULL, pero el CHECK obliga a dejarla
            // en NULL cuando el pais no es Venezuela, de modo que la propia base de datos impide
            // guardar estudiantes nacidos en el extranjero.
            if (estudiante.PaisNacimientoId != Pais.VenezuelaId)
                throw new Exception(
                    "La base de datos no permite registrar estudiantes nacidos en el extranjero: " +
                    "la columna parroquia_nacimiento_id es NOT NULL y la restriccion check_lugar_nacimiento " +
                    "exige dejarla vacia para otros paises. Debe permitirse NULL en esa columna " +
                    "(ALTER TABLE PERSONA_ESTUDIANTE MODIFY parroquia_nacimiento_id INT NULL).");

            if (representante.Id == 0)
            {
                if (string.IsNullOrWhiteSpace(representante.Persona.CedulaIdentidad))
                    throw new Exception("La cedula del representante es obligatoria.");
                if (string.IsNullOrWhiteSpace(representante.Persona.Nombre1) ||
                    string.IsNullOrWhiteSpace(representante.Persona.Apellido1))
                    throw new Exception("El nombre y el apellido del representante son obligatorios.");
                if (string.IsNullOrWhiteSpace(representante.Parentesco))
                    throw new Exception("Indique el parentesco del representante con el estudiante.");
            }

            if (inscripcion.PeriodoId == 0)
                throw new Exception("Seleccione el periodo academico.");
            if (inscripcion.GradoSeccionId == 0)
                throw new Exception("Seleccione el grado y la seccion.");
        }
    }
}
