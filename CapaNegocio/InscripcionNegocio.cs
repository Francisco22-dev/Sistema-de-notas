using System;
using Entidades;
using SistemaLiceo.Datos;

namespace SistemaLiceo.Negocio
{
    public class InscripcionNegocio
    {
        private readonly EstudianteDatos _datos = new EstudianteDatos();

        public int RegistrarInscripcionCompleta(Representante representante, Estudiante estudiante, Inscripcion inscripcion)
        {
            Validar(representante, estudiante, inscripcion);

            if (_datos.ExisteCedulaEscolar(estudiante.CedulaEscolar))
                throw new Exception("La cédula escolar " + estudiante.CedulaEscolar + " ya está registrada.");

            return _datos.RegistrarInscripcionCompleta(representante, estudiante, inscripcion);
        }

        public Estudiante? ObtenerEstudiantePorId(int estudianteId)
        {
            if (estudianteId <= 0)
                throw new ArgumentException("ID de estudiante no válido.");

            return _datos.ObtenerPorId(estudianteId);
        }

        public void ActualizarInscripcionCompleta(Representante representante, Estudiante estudiante, Inscripcion? inscripcion)
        {
            if (estudiante.Id <= 0)
                throw new Exception("El estudiante a actualizar no cuenta con un identificador válido.");

            Validar(representante, estudiante, inscripcion ?? new Inscripcion { PeriodoId = 1, GradoSeccionId = 1 });

            if (_datos.ExisteCedulaEscolar(estudiante.CedulaEscolar, estudiante.Id))
                throw new Exception("La cédula escolar " + estudiante.CedulaEscolar + " ya pertenece a otro estudiante.");

            _datos.ActualizarInscripcionCompleta(representante, estudiante, inscripcion);
        }

        public void RetirarEstudiante(int estudianteId)
        {
            if (estudianteId <= 0)
                throw new ArgumentException("ID de estudiante no válido.");

            _datos.CambiarEstado(estudianteId, "Retirado");
        }

        private static void Validar(Representante representante, Estudiante estudiante, Inscripcion inscripcion)
        {
            // Cédula de Identidad OBLIGATORIA
            if (string.IsNullOrWhiteSpace(estudiante.Persona.CedulaIdentidad))
                throw new Exception("La cédula de identidad del estudiante es obligatoria.");

            // Si la cédula escolar viene vacía, se autoasigna la cédula de identidad para no violar el NOT NULL
            if (string.IsNullOrWhiteSpace(estudiante.CedulaEscolar))
                estudiante.CedulaEscolar = estudiante.Persona.CedulaIdentidad.Trim();

            if (string.IsNullOrWhiteSpace(estudiante.Persona.Nombre1) ||
                string.IsNullOrWhiteSpace(estudiante.Persona.Apellido1))
                throw new Exception("El primer nombre y el primer apellido del estudiante son obligatorios.");

            if (estudiante.Persona.FechaNacimiento == null)
                throw new Exception("Seleccione la fecha de nacimiento del estudiante.");

            if (estudiante.PaisNacimientoId == Pais.VenezuelaId && estudiante.ParroquiaNacimientoId == null)
                throw new Exception("Indique la parroquia de nacimiento del estudiante.");

            if (estudiante.PaisNacimientoId != Pais.VenezuelaId)
                throw new Exception("La base de datos requiere modificar la restricción check_lugar_nacimiento para extranjeros.");

            if (representante.Id == 0)
            {
                if (string.IsNullOrWhiteSpace(representante.Persona.CedulaIdentidad))
                    throw new Exception("La cédula del representante es obligatoria.");
                if (string.IsNullOrWhiteSpace(representante.Persona.Nombre1) ||
                    string.IsNullOrWhiteSpace(representante.Persona.Apellido1))
                    throw new Exception("El nombre y el apellido del representante son obligatorios.");
                if (string.IsNullOrWhiteSpace(representante.Parentesco))
                    throw new Exception("Indique el parentesco del representante con el estudiante.");
            }

            if (inscripcion.PeriodoId == 0)
                throw new Exception("Seleccione el período académico.");
            if (inscripcion.GradoSeccionId == 0)
                throw new Exception("Seleccione el grado y la sección.");
        }
    }
}