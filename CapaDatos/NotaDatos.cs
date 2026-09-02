using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.Collections.Generic;
using System.Data;
using Entidades;
using MySqlConnector;

namespace SistemaLiceo.Datos
{
    /// <summary>
    /// Gestión transaccional y masiva de calificaciones por evaluación, lapsos y períodos
    /// según las tablas evaluacion, nota_evaluacion_lapso, nota_lapso_periodo y nota_periodo_inscripcion.
    /// </summary>
    public class NotaDatos
    {
        private readonly ConexionBD _conexion = new ConexionBD();

        /// <summary>
        /// Obtiene el plan de evaluaciones registradas para una carga docente en un lapso específico.
        /// </summary>
        public DataTable ObtenerPlanEvaluacion(int materiaProfePeriodoId, string lapsoNombre)
        {
            const string consulta = @"
                SELECT DISTINCT ev.id AS EvaluacionId,
                                ev.descripcion AS Descripcion,
                                nel.porcentaje AS Porcentaje
                FROM NOTA_PERIODO_INSCRIPCION npi
                INNER JOIN NOTA_LAPSO_PERIODO nlp ON nlp.nota_periodo_id = npi.id
                INNER JOIN NOTA_EVALUACION_LAPSO nel ON nel.nota_lapso_id = nlp.id
                INNER JOIN EVALUACION ev ON ev.id = nel.evaluacion_id
                WHERE npi.materia_profe_periodo_id = @mppId AND nlp.nombre = @lapso
                ORDER BY ev.id;";

            DataTable tabla = new DataTable();
            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@mppId", materiaProfePeriodoId);
                comando.Parameters.AddWithValue("@lapso", lapsoNombre);

                using (MySqlDataAdapter adaptador = new MySqlDataAdapter(comando))
                {
                    adaptador.Fill(tabla);
                }
            }
            return tabla;
        }

        /// <summary>
        /// Obtiene los alumnos inscritos en la sección con su nota actual en la evaluación consultada.
        /// </summary>
        public List<CalificacionEstudianteDto> ObtenerEstudiantesParaCargaNotas(int materiaProfePeriodoId, string lapsoNombre, string descripcionEvaluacion)
        {
            List<CalificacionEstudianteDto> estudiantes = new List<CalificacionEstudianteDto>();

            const string consulta = @"
                SELECT i.id AS InscripcionId,
                       pe.id AS EstudianteId,
                       pe.cedula_escolar AS CedulaEscolar,
                       CONCAT(p.nacionalidad, '-', IFNULL(p.cedula_identidad, 'S/C')) AS Cedula,
                       CONCAT_WS(' ', p.apellido_1, p.apellido_2, p.nombre_1, p.nombre_2) AS NombreCompleto,
                       ev.id AS EvaluacionId,
                       IFNULL(ev.nota, 0) AS NotaEvaluacion,
                       nlp.nota AS NotaDefinitivaLapso
                FROM MATERIA_PROFESOR_PERIODO mpp
                INNER JOIN INSCRIPCION i ON i.grado_seccion_id = mpp.grado_seccion_id AND i.periodo_id = mpp.periodo_id
                INNER JOIN PERSONA_ESTUDIANTE pe ON pe.id = i.estudiante_id
                INNER JOIN PERSONA p ON p.id = pe.persona_id
                LEFT JOIN NOTA_PERIODO_INSCRIPCION npi ON npi.inscripcion_id = i.id AND npi.materia_profe_periodo_id = mpp.id
                LEFT JOIN NOTA_LAPSO_PERIODO nlp ON nlp.nota_periodo_id = npi.id AND nlp.nombre = @lapso
                LEFT JOIN NOTA_EVALUACION_LAPSO nel ON nel.nota_lapso_id = nlp.id
                LEFT JOIN EVALUACION ev ON ev.id = nel.evaluacion_id AND ev.descripcion = @descripcion
                WHERE mpp.id = @mppId AND pe.ESTADO = 'Activo'
                ORDER BY p.apellido_1, p.nombre_1;";

            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@mppId", materiaProfePeriodoId);
                comando.Parameters.AddWithValue("@lapso", lapsoNombre);
                comando.Parameters.AddWithValue("@descripcion", descripcionEvaluacion);

                using (MySqlDataReader lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        estudiantes.Add(new CalificacionEstudianteDto
                        {
                            InscripcionId = lector.GetInt32("InscripcionId"),
                            EstudianteId = lector.GetInt32("EstudianteId"),
                            CedulaEscolar = lector.GetString("CedulaEscolar"),
                            Cedula = lector.GetString("Cedula"),
                            NombreCompleto = lector.GetString("NombreCompleto"),
                            EvaluacionId = lector.IsDBNull("EvaluacionId") ? null : lector.GetInt32("EvaluacionId"),
                            NotaEvaluacion = lector.GetInt32("NotaEvaluacion"),
                            NotaDefinitivaLapso = lector.IsDBNull("NotaDefinitivaLapso") ? null : lector.GetInt32("NotaDefinitivaLapso")
                        });
                    }
                }
            }

            return estudiantes;
        }

        /// <summary>
        /// Guarda o actualiza masivamente las notas de una evaluación para toda la sección en una sola transacción.
        /// </summary>
        public void GuardarNotasSeccionMasiva(
            int materiaProfePeriodoId,
            string lapsoNombre,
            string descripcionEvaluacion,
            int porcentaje,
            List<CalificacionEstudianteDto> listaNotas)
        {
            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlTransaction transaccion = conexion.BeginTransaction())
            {
                try
                {
                    foreach (var item in listaNotas)
                    {
                        // 1. Obtener o crear la cabecera NOTA_PERIODO_INSCRIPCION
                        int notaPeriodoId = ObtenerOCrearNotaPeriodo(item.InscripcionId, materiaProfePeriodoId, conexion, transaccion);

                        // 2. Obtener o crear la cabecera NOTA_LAPSO_PERIODO
                        int notaLapsoId = ObtenerOCrearNotaLapso(notaPeriodoId, lapsoNombre, conexion, transaccion);

                        // 3. Crear o actualizar el registro puntual en EVALUACION
                        int evaluacionId = GuardarOActualizarEvaluacion(item.EvaluacionId, descripcionEvaluacion, item.NotaEvaluacion, conexion, transaccion);

                        // 4. Vincular EVALUACION a NOTA_EVALUACION_LAPSO
                        VincularEvaluacionLapso(notaLapsoId, evaluacionId, porcentaje, conexion, transaccion);

                        // 5. Recalcular y actualizar la nota acumulada/definitiva del lapso
                        ActualizarPromedioLapso(notaLapsoId, conexion, transaccion);
                    }

                    transaccion.Commit();
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

        private static int ObtenerOCrearNotaPeriodo(int inscripcionId, int mppId, MySqlConnection conexion, MySqlTransaction transaccion)
        {
            const string busqueda = @"
                SELECT id FROM NOTA_PERIODO_INSCRIPCION 
                WHERE inscripcion_id = @ins AND materia_profe_periodo_id = @mpp LIMIT 1;";

            using (MySqlCommand comando = new MySqlCommand(busqueda, conexion, transaccion))
            {
                comando.Parameters.AddWithValue("@ins", inscripcionId);
                comando.Parameters.AddWithValue("@mpp", mppId);
                object? res = comando.ExecuteScalar();
                if (res != null && res != DBNull.Value)
                    return Convert.ToInt32(res);
            }

            const string insercion = @"
                INSERT INTO NOTA_PERIODO_INSCRIPCION (inscripcion_id, materia_profe_periodo_id, nota)
                VALUES (@ins, @mpp, 0);
                SELECT LAST_INSERT_ID();";

            using (MySqlCommand comando = new MySqlCommand(insercion, conexion, transaccion))
            {
                comando.Parameters.AddWithValue("@ins", inscripcionId);
                comando.Parameters.AddWithValue("@mpp", mppId);
                return Convert.ToInt32(comando.ExecuteScalar());
            }
        }

        private static int ObtenerOCrearNotaLapso(int notaPeriodoId, string lapsoNombre, MySqlConnection conexion, MySqlTransaction transaccion)
        {
            const string busqueda = @"
                SELECT id FROM NOTA_LAPSO_PERIODO 
                WHERE nota_periodo_id = @npId AND nombre = @nombre LIMIT 1;";

            using (MySqlCommand comando = new MySqlCommand(busqueda, conexion, transaccion))
            {
                comando.Parameters.AddWithValue("@npId", notaPeriodoId);
                comando.Parameters.AddWithValue("@nombre", lapsoNombre);
                object? res = comando.ExecuteScalar();
                if (res != null && res != DBNull.Value)
                    return Convert.ToInt32(res);
            }

            const string insercion = @"
                INSERT INTO NOTA_LAPSO_PERIODO (nombre, nota_periodo_id, nota)
                VALUES (@nombre, @npId, 0);
                SELECT LAST_INSERT_ID();";

            using (MySqlCommand comando = new MySqlCommand(insercion, conexion, transaccion))
            {
                comando.Parameters.AddWithValue("@nombre", lapsoNombre);
                comando.Parameters.AddWithValue("@npId", notaPeriodoId);
                return Convert.ToInt32(comando.ExecuteScalar());
            }
        }

        private static int GuardarOActualizarEvaluacion(int? evaluacionId, string descripcion, int nota, MySqlConnection conexion, MySqlTransaction transaccion)
        {
            if (evaluacionId.HasValue && evaluacionId.Value > 0)
            {
                const string update = "UPDATE EVALUACION SET nota = @nota, descripcion = @desc WHERE id = @id;";
                using (MySqlCommand comando = new MySqlCommand(update, conexion, transaccion))
                {
                    comando.Parameters.AddWithValue("@id", evaluacionId.Value);
                    comando.Parameters.AddWithValue("@nota", nota);
                    comando.Parameters.AddWithValue("@desc", descripcion);
                    comando.ExecuteNonQuery();
                    return evaluacionId.Value;
                }
            }

            const string insert = @"
                INSERT INTO EVALUACION (descripcion, nota)
                VALUES (@desc, @nota);
                SELECT LAST_INSERT_ID();";

            using (MySqlCommand comando = new MySqlCommand(insert, conexion, transaccion))
            {
                comando.Parameters.AddWithValue("@desc", descripcion);
                comando.Parameters.AddWithValue("@nota", nota);
                return Convert.ToInt32(comando.ExecuteScalar());
            }
        }

        private static void VincularEvaluacionLapso(int notaLapsoId, int evaluacionId, int porcentaje, MySqlConnection conexion, MySqlTransaction transaccion)
        {
            const string consulta = @"
                INSERT INTO NOTA_EVALUACION_LAPSO (nota_lapso_id, evaluacion_id, porcentaje)
                VALUES (@lapsoId, @evalId, @porcentaje)
                ON DUPLICATE KEY UPDATE porcentaje = VALUES(porcentaje);";

            using (MySqlCommand comando = new MySqlCommand(consulta, conexion, transaccion))
            {
                comando.Parameters.AddWithValue("@lapsoId", notaLapsoId);
                comando.Parameters.AddWithValue("@evalId", evaluacionId);
                comando.Parameters.AddWithValue("@porcentaje", porcentaje);
                comando.ExecuteNonQuery();
            }
        }

        private static void ActualizarPromedioLapso(int notaLapsoId, MySqlConnection conexion, MySqlTransaction transaccion)
        {
            // Calcula la sumatoria ponderada: ROUND(SUM(ev.nota * (nel.porcentaje / 100)))
            const string calculo = @"
                UPDATE NOTA_LAPSO_PERIODO nlp
                SET nlp.nota = IFNULL((
                    SELECT ROUND(SUM(e.nota * (nel.porcentaje / 100.0)))
                    FROM NOTA_EVALUACION_LAPSO nel
                    INNER JOIN EVALUACION e ON e.id = nel.evaluacion_id
                    WHERE nel.nota_lapso_id = @lapsoId
                ), 0)
                WHERE nlp.id = @lapsoId;";

            using (MySqlCommand comando = new MySqlCommand(calculo, conexion, transaccion))
            {
                comando.Parameters.AddWithValue("@lapsoId", notaLapsoId);
                comando.ExecuteNonQuery();
            }
        }
        public List<FilaPlanillaNotasDto> ObtenerPlanillaLapso(int materiaProfePeriodoId, string lapsoNombre)
        {
            List<FilaPlanillaNotasDto> lista = new List<FilaPlanillaNotasDto>();

            const string consulta = @"
        SELECT i.id AS InscripcionId,
               pe.id AS EstudianteId,
               CONCAT(p.nacionalidad, '-', IFNULL(p.cedula_identidad, 'S/C')) AS Cedula,
               CONCAT_WS(' ', p.apellido_1, p.apellido_2, p.nombre_1, p.nombre_2) AS Estudiante
        FROM MATERIA_PROFESOR_PERIODO mpp
        INNER JOIN INSCRIPCION i ON i.grado_seccion_id = mpp.grado_seccion_id AND i.periodo_id = mpp.periodo_id
        INNER JOIN PERSONA_ESTUDIANTE pe ON pe.id = i.estudiante_id
        INNER JOIN PERSONA p ON p.id = pe.persona_id
        WHERE mpp.id = @mppId AND pe.ESTADO = 'Activo'
        ORDER BY p.apellido_1, p.nombre_1;";

            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@mppId", materiaProfePeriodoId);
                using (MySqlDataReader lector = comando.ExecuteReader())
                {
                    int nro = 1;
                    while (lector.Read())
                    {
                        lista.Add(new FilaPlanillaNotasDto
                        {
                            NroLista = nro++,
                            InscripcionId = lector.GetInt32("InscripcionId"),
                            EstudianteId = lector.GetInt32("EstudianteId"),
                            Cedula = lector.GetString("Cedula"),
                            ApellidosYNombres = lector.GetString("Estudiante").ToUpper()
                        });
                    }
                }
            }

            // Cargar las notas existentes de cada evaluación en este lapso
            foreach (var fila in lista)
            {
                CargarNotasEvaluacionesAlumno(fila, materiaProfePeriodoId, lapsoNombre);
                fila.Recalcular();
            }

            return lista;
        }

        private void CargarNotasEvaluacionesAlumno(FilaPlanillaNotasDto fila, int mppId, string lapsoNombre)
        {
            const string consulta = @"
        SELECT ev.descripcion, ev.nota, nel.porcentaje
        FROM NOTA_PERIODO_INSCRIPCION npi
        INNER JOIN NOTA_LAPSO_PERIODO nlp ON nlp.nota_periodo_id = npi.id
        INNER JOIN NOTA_EVALUACION_LAPSO nel ON nel.nota_lapso_id = nlp.id
        INNER JOIN EVALUACION ev ON ev.id = nel.evaluacion_id
        WHERE npi.inscripcion_id = @insId AND npi.materia_profe_periodo_id = @mppId AND nlp.nombre = @lapso
        ORDER BY ev.id ASC LIMIT 6;";

            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@insId", fila.InscripcionId);
                comando.Parameters.AddWithValue("@mppId", mppId);
                comando.Parameters.AddWithValue("@lapso", lapsoNombre);

                using (MySqlDataReader lector = comando.ExecuteReader())
                {
                    int idx = 1;
                    while (lector.Read())
                    {
                        decimal nota = Convert.ToDecimal(lector["nota"]);
                        switch (idx)
                        {
                            case 1: fila.Eval1 = nota; break;
                            case 2: fila.Eval2 = nota; break;
                            case 3: fila.Eval3 = nota; break;
                            case 4: fila.Eval4 = nota; break;
                            case 5: fila.Eval5 = nota; break;
                            case 6: fila.Eval6 = nota; break;
                        }
                        idx++;
                    }
                }
            }
        }

        /// <summary>
        /// Guarda toda la planilla matricial (evaluaciones 1 a 6 + notas definitivas) en una sola transacción.
        /// </summary>
        public void GuardarPlanillaCompleta(
            int materiaProfePeriodoId,
            string lapsoNombre,
            string[] nombresEvaluaciones,
            decimal[] ponderaciones,
            List<FilaPlanillaNotasDto> filas)
        {
            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlTransaction transaccion = conexion.BeginTransaction())
            {
                try
                {
                    foreach (var fila in filas)
                    {
                        // 1. Cabeceras
                        int notaPeriodoId = ObtenerOCrearNotaPeriodo(fila.InscripcionId, materiaProfePeriodoId, conexion, transaccion);
                        int notaLapsoId = ObtenerOCrearNotaLapso(notaPeriodoId, lapsoNombre, conexion, transaccion);

                        // 2. Guardar cada evaluación no nula
                        decimal?[] notas = new decimal?[] { fila.Eval1, fila.Eval2, fila.Eval3, fila.Eval4, fila.Eval5, fila.Eval6 };

                        for (int i = 0; i < notas.Length; i++)
                        {
                            if (notas[i].HasValue)
                            {
                                string desc = (i < nombresEvaluaciones.Length && !string.IsNullOrWhiteSpace(nombresEvaluaciones[i]))
                                    ? nombresEvaluaciones[i]
                                    : $"Evaluación {i + 1}";

                                int porc = (i < ponderaciones.Length) ? (int)ponderaciones[i] : 20;
                                int notaEntera = (int)Math.Round(notas[i]!.Value);

                                int evalId = GuardarOActualizarEvaluacionPorDescripcion(notaLapsoId, desc, notaEntera, conexion, transaccion);
                                VincularEvaluacionLapso(notaLapsoId, evalId, porc, conexion, transaccion);
                            }
                        }

                        // 3. Fijar la Definitiva del Lapso directamente de la sumatoria calculada
                        const string updateLapso = "UPDATE NOTA_LAPSO_PERIODO SET nota = @def WHERE id = @id;";
                        using (MySqlCommand cmdLapso = new MySqlCommand(updateLapso, conexion, transaccion))
                        {
                            cmdLapso.Parameters.AddWithValue("@def", fila.Definitiva);
                            cmdLapso.Parameters.AddWithValue("@id", notaLapsoId);
                            cmdLapso.ExecuteNonQuery();
                        }

                        // 4. Actualizar la Definitiva Anual del Período (Promedio de los lapsos)
                        const string updatePeriodo = @"
                    UPDATE NOTA_PERIODO_INSCRIPCION npi
                    SET npi.nota = ROUND((SELECT AVG(nlp.nota) FROM NOTA_LAPSO_PERIODO nlp WHERE nlp.nota_periodo_id = npi.id AND nlp.nombre <> 'Reparacion'))
                    WHERE npi.id = @npId;";
                        using (MySqlCommand cmdPer = new MySqlCommand(updatePeriodo, conexion, transaccion))
                        {
                            cmdPer.Parameters.AddWithValue("@npId", notaPeriodoId);
                            cmdPer.ExecuteNonQuery();
                        }
                    }

                    transaccion.Commit();
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

        private static int GuardarOActualizarEvaluacionPorDescripcion(int notaLapsoId, string descripcion, int nota, MySqlConnection conexion, MySqlTransaction transaccion)
        {
            const string busqueda = @"
        SELECT ev.id FROM EVALUACION ev
        INNER JOIN NOTA_EVALUACION_LAPSO nel ON nel.evaluacion_id = ev.id
        WHERE nel.nota_lapso_id = @lapsoId AND ev.descripcion = @desc LIMIT 1;";

            using (MySqlCommand cmd = new MySqlCommand(busqueda, conexion, transaccion))
            {
                cmd.Parameters.AddWithValue("@lapsoId", notaLapsoId);
                cmd.Parameters.AddWithValue("@desc", descripcion);
                object? res = cmd.ExecuteScalar();

                if (res != null && res != DBNull.Value)
                {
                    int idExistente = Convert.ToInt32(res);
                    const string update = "UPDATE EVALUACION SET nota = @nota WHERE id = @id;";
                    using (MySqlCommand cmdUp = new MySqlCommand(update, conexion, transaccion))
                    {
                        cmdUp.Parameters.AddWithValue("@nota", nota);
                        cmdUp.Parameters.AddWithValue("@id", idExistente);
                        cmdUp.ExecuteNonQuery();
                    }
                    return idExistente;
                }
            }

            const string insert = "INSERT INTO EVALUACION (descripcion, nota) VALUES (@desc, @nota); SELECT LAST_INSERT_ID();";
            using (MySqlCommand cmdIn = new MySqlCommand(insert, conexion, transaccion))
            {
                cmdIn.Parameters.AddWithValue("@desc", descripcion);
                cmdIn.Parameters.AddWithValue("@nota", nota);
                return Convert.ToInt32(cmdIn.ExecuteScalar());
            }
        }
    }
}
