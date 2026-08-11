using System;
using System.Data;
using Entidades;
using MySqlConnector;

namespace SistemaLiceo.Datos
{
    /// <summary>Acceso a PERSONA_ESTUDIANTE, sus tablas de apoyo y la matricula (INSCRIPCION).</summary>
    public class EstudianteDatos
    {
        private readonly ConexionBD _conexion = new ConexionBD();

        /// <summary>
        /// Registra en una sola transaccion: representante (si es nuevo), persona del estudiante,
        /// antropometricos, salud, extra curricular, la ficha del estudiante y su inscripcion.
        /// </summary>
        public int RegistrarInscripcionCompleta(Representante representante, Estudiante estudiante, Inscripcion inscripcion)
        {
            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlTransaction transaccion = conexion.BeginTransaction())
            {
                try
                {
                    // 1. Representante legal (solo si todavia no existe en la base de datos)
                    if (representante.Id == 0)
                        RepresentanteDatos.Insertar(representante, conexion, transaccion);

                    estudiante.RepresentantePrincipalId = representante.Id;

                    // 2. Persona del estudiante (crea tambien su direccion si viene cargada)
                    if (estudiante.PersonaId == 0)
                        estudiante.PersonaId = PersonaDatos.InsertarPersona(estudiante.Persona, conexion, transaccion);

                    // 3. Tablas de apoyo obligatorias en PERSONA_ESTUDIANTE
                    estudiante.AntropometricoId = InsertarAntropometricos(estudiante.Antropometricos, conexion, transaccion);
                    estudiante.SaludId = InsertarSalud(estudiante.Salud, conexion, transaccion);
                    estudiante.ExtraCurricularId = InsertarExtraCurricular(estudiante.ExtraCurricular, conexion, transaccion);

                    // 4. Ficha del estudiante
                    estudiante.Id = InsertarEstudiante(estudiante, conexion, transaccion);

                    // 5. Matricula del periodo
                    inscripcion.EstudianteId = estudiante.Id;
                    inscripcion.Id = InsertarInscripcion(inscripcion, conexion, transaccion);

                    transaccion.Commit();
                    return estudiante.Id;
                }
                catch (MySqlException ex)
                {
                    transaccion.Rollback();
                    throw new Exception(ConexionBD.TraducirError(ex), ex);
                }
                catch
                {
                    transaccion.Rollback();
                    throw;
                }
            }
        }

        /// <summary>Inscribe en un periodo a un estudiante que ya existe.</summary>
        public int RegistrarInscripcion(Inscripcion inscripcion)
        {
            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlTransaction transaccion = conexion.BeginTransaction())
            {
                try
                {
                    int id = InsertarInscripcion(inscripcion, conexion, transaccion);
                    transaccion.Commit();
                    return id;
                }
                catch (MySqlException ex)
                {
                    transaccion.Rollback();
                    throw new Exception(ConexionBD.TraducirError(ex), ex);
                }
                catch
                {
                    transaccion.Rollback();
                    throw;
                }
            }
        }

        private static int InsertarAntropometricos(Antropometricos datos, MySqlConnection conexion, MySqlTransaction transaccion)
        {
            const string consulta = @"
                INSERT INTO ANTROPOMETRICOS (estatura, peso, talla_camisa, talla_pantalon, talla_zapato)
                VALUES (@estatura, @peso, @camisa, @pantalon, @zapato);
                SELECT LAST_INSERT_ID();";

            using (MySqlCommand comando = new MySqlCommand(consulta, conexion, transaccion))
            {
                comando.Parameters.AddWithValue("@estatura", (object?)datos.Estatura ?? DBNull.Value);
                comando.Parameters.AddWithValue("@peso", (object?)datos.Peso ?? DBNull.Value);
                comando.Parameters.AddWithValue("@camisa", PersonaDatos.Nulo(datos.TallaCamisa));
                comando.Parameters.AddWithValue("@pantalon", PersonaDatos.Nulo(datos.TallaPantalon));
                comando.Parameters.AddWithValue("@zapato", (object?)datos.TallaZapato ?? DBNull.Value);

                datos.Id = Convert.ToInt32(comando.ExecuteScalar());
                return datos.Id;
            }
        }

        private static int InsertarSalud(Salud salud, MySqlConnection conexion, MySqlTransaction transaccion)
        {
            const string consulta = @"
                INSERT INTO SALUD (reacciones_alergicas, cuales_alergias, enfermedades_padecidas, atencion_especial,
                                   horario_tratamiento, atendido_por_especialista, nombre_especialista,
                                   fecha_inicio_especialista, condicion_atencion)
                VALUES (@alergicas, @cuales, @enfermedades, @atencion, @horario, @especialista,
                        @nombreEspecialista, @fechaEspecialista, @condicion);
                SELECT LAST_INSERT_ID();";

            using (MySqlCommand comando = new MySqlCommand(consulta, conexion, transaccion))
            {
                comando.Parameters.AddWithValue("@alergicas", salud.ReaccionesAlergicas);
                comando.Parameters.AddWithValue("@cuales", PersonaDatos.Nulo(salud.CualesAlergias));
                comando.Parameters.AddWithValue("@enfermedades", PersonaDatos.Nulo(salud.EnfermedadesPadecidas));
                comando.Parameters.AddWithValue("@atencion", salud.AtencionEspecial);
                comando.Parameters.AddWithValue("@horario", PersonaDatos.Nulo(salud.HorarioTratamiento));
                comando.Parameters.AddWithValue("@especialista", salud.AtendidoPorEspecialista);
                comando.Parameters.AddWithValue("@nombreEspecialista", PersonaDatos.Nulo(salud.NombreEspecialista));
                comando.Parameters.AddWithValue("@fechaEspecialista", (object?)salud.FechaInicioEspecialista ?? DBNull.Value);
                comando.Parameters.AddWithValue("@condicion", PersonaDatos.Nulo(salud.CondicionAtencion));

                salud.Id = Convert.ToInt32(comando.ExecuteScalar());
                return salud.Id;
            }
        }

        private static int InsertarExtraCurricular(ExtraCurricular datos, MySqlConnection conexion, MySqlTransaction transaccion)
        {
            const string consulta = @"
                INSERT INTO EXTRA_CURRICULAR (realiza_deportes, cuales_deportes, posee_canaima, fecha_asignacion_canaima,
                                              serial_canaima, estado_canaima, falla_canaima, posee_cargador,
                                              estado_cargador, falla_cargador)
                VALUES (@deportes, @cuales, @canaima, @fechaCanaima, @serial, @estadoCanaima, @fallaCanaima,
                        @cargador, @estadoCargador, @fallaCargador);
                SELECT LAST_INSERT_ID();";

            using (MySqlCommand comando = new MySqlCommand(consulta, conexion, transaccion))
            {
                comando.Parameters.AddWithValue("@deportes", datos.RealizaDeportes);
                comando.Parameters.AddWithValue("@cuales", PersonaDatos.Nulo(datos.CualesDeportes));
                comando.Parameters.AddWithValue("@canaima", datos.PoseeCanaima);
                comando.Parameters.AddWithValue("@fechaCanaima", (object?)datos.FechaAsignacionCanaima ?? DBNull.Value);
                comando.Parameters.AddWithValue("@serial", PersonaDatos.Nulo(datos.SerialCanaima));
                comando.Parameters.AddWithValue("@estadoCanaima", PersonaDatos.Nulo(datos.EstadoCanaima));
                comando.Parameters.AddWithValue("@fallaCanaima", PersonaDatos.Nulo(datos.FallaCanaima));
                comando.Parameters.AddWithValue("@cargador", datos.PoseeCargador);
                comando.Parameters.AddWithValue("@estadoCargador", PersonaDatos.Nulo(datos.EstadoCargador));
                comando.Parameters.AddWithValue("@fallaCargador", PersonaDatos.Nulo(datos.FallaCargador));

                datos.Id = Convert.ToInt32(comando.ExecuteScalar());
                return datos.Id;
            }
        }

        private static int InsertarEstudiante(Estudiante estudiante, MySqlConnection conexion, MySqlTransaction transaccion)
        {
            const string consulta = @"
                INSERT INTO PERSONA_ESTUDIANTE (cedula_escolar, numero_hijo, lateralidad, persona_id,
                                                pais_nacimiento_id, parroquia_nacimiento_id, antropometrico_id,
                                                salud_id, extra_curricular_id, representante_principal_id,
                                                representante_secundario_id, ESTADO)
                VALUES (@cedulaEscolar, @numeroHijo, @lateralidad, @persona, @pais, @parroquia, @antropometrico,
                        @salud, @extra, @representante, @representanteSecundario, @estado);
                SELECT LAST_INSERT_ID();";

            using (MySqlCommand comando = new MySqlCommand(consulta, conexion, transaccion))
            {
                comando.Parameters.AddWithValue("@cedulaEscolar", estudiante.CedulaEscolar);
                comando.Parameters.AddWithValue("@numeroHijo", estudiante.NumeroHijo);
                comando.Parameters.AddWithValue("@lateralidad", estudiante.Lateralidad);
                comando.Parameters.AddWithValue("@persona", estudiante.PersonaId);
                comando.Parameters.AddWithValue("@pais", estudiante.PaisNacimientoId);
                comando.Parameters.AddWithValue("@parroquia", (object?)estudiante.ParroquiaNacimientoId ?? DBNull.Value);
                comando.Parameters.AddWithValue("@antropometrico", estudiante.AntropometricoId);
                comando.Parameters.AddWithValue("@salud", estudiante.SaludId);
                comando.Parameters.AddWithValue("@extra", estudiante.ExtraCurricularId);
                comando.Parameters.AddWithValue("@representante", estudiante.RepresentantePrincipalId);
                comando.Parameters.AddWithValue("@representanteSecundario", (object?)estudiante.RepresentanteSecundarioId ?? DBNull.Value);
                comando.Parameters.AddWithValue("@estado", estudiante.Estado);

                return Convert.ToInt32(comando.ExecuteScalar());
            }
        }

        private static int InsertarInscripcion(Inscripcion inscripcion, MySqlConnection conexion, MySqlTransaction transaccion)
        {
            const string consulta = @"
                INSERT INTO INSCRIPCION (periodo_id, estudiante_id, grado_seccion_id, tipo_ingreso,
                                         colegio_procedencia, nivel_academico, fecha_inscripcion)
                VALUES (@periodo, @estudiante, @gradoSeccion, @tipoIngreso, @colegio, @nivel, @fecha);
                SELECT LAST_INSERT_ID();";

            using (MySqlCommand comando = new MySqlCommand(consulta, conexion, transaccion))
            {
                comando.Parameters.AddWithValue("@periodo", inscripcion.PeriodoId);
                comando.Parameters.AddWithValue("@estudiante", inscripcion.EstudianteId);
                comando.Parameters.AddWithValue("@gradoSeccion", inscripcion.GradoSeccionId);
                comando.Parameters.AddWithValue("@tipoIngreso", inscripcion.TipoIngreso);
                comando.Parameters.AddWithValue("@colegio", PersonaDatos.Nulo(inscripcion.ColegioProcedencia));
                comando.Parameters.AddWithValue("@nivel", inscripcion.NivelAcademico);
                comando.Parameters.AddWithValue("@fecha", inscripcion.FechaInscripcion);

                return Convert.ToInt32(comando.ExecuteScalar());
            }
        }

        /// <summary>Estudiantes activos con su matricula del periodo indicado (o del ultimo periodo activo).</summary>
        public DataTable ObtenerEstudiantesActivos(int periodoId = 0)
        {
            const string consulta = @"
                SELECT e.id AS Codigo,
                       e.cedula_escolar AS 'Cedula Escolar',
                       CONCAT(p.nacionalidad, '-', IFNULL(p.cedula_identidad, 'S/C')) AS Cedula,
                       CONCAT_WS(' ', p.nombre_1, p.nombre_2, p.apellido_1, p.apellido_2) AS Estudiante,
                       p.sexo AS Sexo,
                       p.fecha_nacimiento AS 'Fecha de Nacimiento',
                       g.nombre AS Grado,
                       s.nombre AS Seccion,
                       pa.nombre AS Periodo,
                       i.tipo_ingreso AS 'Tipo de Ingreso',
                       CONCAT_WS(' ', pr.nombre_1, pr.apellido_1) AS Representante,
                       r.telefono_movil AS 'Telefono Representante'
                FROM PERSONA_ESTUDIANTE e
                INNER JOIN PERSONA p ON p.id = e.persona_id
                INNER JOIN PERSONA_REPRESENTANTE r ON r.id = e.representante_principal_id
                INNER JOIN PERSONA pr ON pr.id = r.persona_id
                LEFT JOIN INSCRIPCION i ON i.estudiante_id = e.id
                     AND (@periodo = 0 OR i.periodo_id = @periodo)
                LEFT JOIN PERIODO_ACADEMICO pa ON pa.id = i.periodo_id
                LEFT JOIN GRADO_SECCION gs ON gs.id = i.grado_seccion_id
                LEFT JOIN GRADO g ON g.id = gs.grado_id
                LEFT JOIN SECCION s ON s.id = gs.seccion_id
                WHERE e.ESTADO = 'Activo'
                ORDER BY p.apellido_1, p.nombre_1;";

            DataTable tabla = new DataTable();
            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@periodo", periodoId);
                using (MySqlDataAdapter adaptador = new MySqlDataAdapter(comando))
                {
                    adaptador.Fill(tabla);
                }
            }
            return tabla;
        }

        /// <summary>Indica si la cedula escolar ya esta registrada.</summary>
        public bool ExisteCedulaEscolar(string cedulaEscolar)
        {
            const string consulta = "SELECT 1 FROM PERSONA_ESTUDIANTE WHERE cedula_escolar = @cedula LIMIT 1;";

            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@cedula", cedulaEscolar);
                return comando.ExecuteScalar() != null;
            }
        }
    }
}
