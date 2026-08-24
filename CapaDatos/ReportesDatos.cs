using System;
using System.Collections.Generic;
using System.Data;
using Entidades;
using MySqlConnector;

namespace SistemaLiceo.Datos
{
    public class ReportesDatos
    {
        private readonly ConexionBD _conexion = new ConexionBD();

        public ConstanciaEstudioDto? ObtenerDatosConstancia(int estudianteId, int periodoId)
        {
            const string consulta = @"
                SELECT CONCAT_WS(' ', p.nombre_1, p.nombre_2, p.apellido_1, p.apellido_2) AS Estudiante,
                       CONCAT(p.nacionalidad, '-', IFNULL(p.cedula_identidad, 'S/C')) AS Cedula,
                       pe.cedula_escolar AS CedulaEscolar,
                       g.nombre AS Grado,
                       s.nombre AS Seccion,
                       pa.nombre AS Periodo,
                       i.nivel_academico AS Nivel,
                       i.fecha_inscripcion AS FechaInscripcion
                FROM INSCRIPCION i
                INNER JOIN PERSONA_ESTUDIANTE pe ON pe.id = i.estudiante_id
                INNER JOIN PERSONA p ON p.id = pe.persona_id
                INNER JOIN PERIODO_ACADEMICO pa ON pa.id = i.periodo_id
                INNER JOIN GRADO_SECCION gs ON gs.id = i.grado_seccion_id
                INNER JOIN GRADO g ON g.id = gs.grado_id
                INNER JOIN SECCION s ON s.id = gs.seccion_id
                WHERE pe.id = @estId AND i.periodo_id = @perId
                LIMIT 1;";

            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@estId", estudianteId);
                comando.Parameters.AddWithValue("@perId", periodoId);

                using (MySqlDataReader lector = comando.ExecuteReader())
                {
                    if (!lector.Read()) return null;

                    return new ConstanciaEstudioDto
                    {
                        EstudianteNombreCompleto = lector.GetString("Estudiante"),
                        Cedula = lector.GetString("Cedula"),
                        CedulaEscolar = lector.GetString("CedulaEscolar"),
                        Grado = lector.GetString("Grado"),
                        Seccion = lector.GetString("Seccion"),
                        Periodo = lector.GetString("Periodo"),
                        NivelAcademico = lector.GetString("Nivel"),
                        FechaInscripcion = lector.GetDateTime("FechaInscripcion")
                    };
                }
            }
        }

        public List<FilaBoletaDto> ObtenerBoletaNotas(int estudianteId, int periodoId)
        {
            List<FilaBoletaDto> filas = new List<FilaBoletaDto>();

            const string consulta = @"
                SELECT m.nombre AS Materia,
                       CONCAT_WS(' ', pdoc.nombre_1, pdoc.apellido_1) AS Docente,
                       MAX(CASE WHEN nlp.nombre = '1er lapso' THEN nlp.nota END) AS Lapso1,
                       MAX(CASE WHEN nlp.nombre = '2do lapso' THEN nlp.nota END) AS Lapso2,
                       MAX(CASE WHEN nlp.nombre = '3er lapso' THEN nlp.nota END) AS Lapso3,
                       npi.nota AS Definitiva
                FROM INSCRIPCION i
                INNER JOIN MATERIA_PROFESOR_PERIODO mpp ON mpp.grado_seccion_id = i.grado_seccion_id AND mpp.periodo_id = i.periodo_id
                INNER JOIN GRADO_MATERIA gm ON gm.id = mpp.grado_materia_id
                INNER JOIN MATERIA m ON m.id = gm.materia_id
                INNER JOIN MATERIA_PROFESOR mp ON mp.id = mpp.materia_profesor_id
                INNER JOIN PROFESOR prof ON prof.id = mp.profesor_id
                INNER JOIN PERSONA pdoc ON pdoc.id = prof.persona_id
                LEFT JOIN NOTA_PERIODO_INSCRIPCION npi ON npi.inscripcion_id = i.id AND npi.materia_profe_periodo_id = mpp.id
                LEFT JOIN NOTA_LAPSO_PERIODO nlp ON nlp.nota_periodo_id = npi.id
                WHERE i.estudiante_id = @estId AND i.periodo_id = @perId
                GROUP BY m.id, m.nombre, pdoc.id, npi.nota
                ORDER BY m.nombre;";

            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@estId", estudianteId);
                comando.Parameters.AddWithValue("@perId", periodoId);

                using (MySqlDataReader lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        filas.Add(new FilaBoletaDto
                        {
                            Materia = lector.GetString("Materia"),
                            Docente = lector.GetString("Docente"),
                            NotaLapso1 = lector.IsDBNull(lector.GetOrdinal("Lapso1")) ? null : lector.GetInt32("Lapso1"),
                            NotaLapso2 = lector.IsDBNull(lector.GetOrdinal("Lapso2")) ? null : lector.GetInt32("Lapso2"),
                            NotaLapso3 = lector.IsDBNull(lector.GetOrdinal("Lapso3")) ? null : lector.GetInt32("Lapso3"),
                            NotaDefinitiva = lector.IsDBNull(lector.GetOrdinal("Definitiva")) ? null : lector.GetInt32("Definitiva")
                        });
                    }
                }
            }

            return filas;
        }

        public List<FilaNominaSeccionDto> ObtenerNominaSeccion(int gradoSeccionId, int periodoId)
        {
            List<FilaNominaSeccionDto> lista = new List<FilaNominaSeccionDto>();

            const string consulta = @"
                SELECT pe.cedula_escolar AS CedulaEscolar,
                       CONCAT(p.nacionalidad, '-', IFNULL(p.cedula_identidad, 'S/C')) AS Cedula,
                       CONCAT_WS(' ', p.apellido_1, p.apellido_2, p.nombre_1, p.nombre_2) AS Estudiante,
                       p.sexo AS Sexo,
                       CONCAT_WS(' ', prep.nombre_1, prep.apellido_1) AS Representante,
                       r.telefono_movil AS Telefono
                FROM INSCRIPCION i
                INNER JOIN PERSONA_ESTUDIANTE pe ON pe.id = i.estudiante_id
                INNER JOIN PERSONA p ON p.id = pe.persona_id
                INNER JOIN PERSONA_REPRESENTANTE r ON r.id = pe.representante_principal_id
                INNER JOIN PERSONA prep ON prep.id = r.persona_id
                WHERE i.grado_seccion_id = @gsId AND i.periodo_id = @perId AND pe.ESTADO = 'Activo'
                ORDER BY p.apellido_1, p.nombre_1;";

            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@gsId", gradoSeccionId);
                comando.Parameters.AddWithValue("@perId", periodoId);

                using (MySqlDataReader lector = comando.ExecuteReader())
                {
                    int nro = 1;
                    while (lector.Read())
                    {
                        lista.Add(new FilaNominaSeccionDto
                        {
                            Numero = nro++,
                            CedulaEscolar = lector.GetString("CedulaEscolar"),
                            Cedula = lector.GetString("Cedula"),
                            Estudiante = lector.GetString("Estudiante"),
                            Sexo = lector.GetString("Sexo"),
                            Representante = lector.GetString("Representante"),
                            TelefonoRepresentante = lector.IsDBNull(lector.GetOrdinal("Telefono")) ? string.Empty : lector.GetString("Telefono")
                        });
                    }
                }
            }

            return lista;
        }
    }
}