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
        public List<FilaSazeMatriculaDto> ObtenerSazeMatriculaInicial(int gradoSeccionId, int periodoId)
        {
            List<FilaSazeMatriculaDto> lista = new List<FilaSazeMatriculaDto>();

            const string consulta = @"
        SELECT pe.cedula_escolar AS CedulaEscolar,
               CONCAT(p.nacionalidad, '-', IFNULL(p.cedula_identidad, 'S/C')) AS Cedula,
               p.nombre_1 AS Nombre1, p.nombre_2 AS Nombre2,
               p.apellido_1 AS Apellido1, p.apellido_2 AS Apellido2,
               p.sexo AS Sexo,
               p.fecha_nacimiento AS FechaNacimiento,
               TIMESTAMPDIFF(YEAR, p.fecha_nacimiento, CURDATE()) AS Edad,
               IFNULL(parr.nombre, pais.nombre) AS LugarNacimiento,
               i.tipo_ingreso AS TipoIngreso
        FROM INSCRIPCION i
        INNER JOIN PERSONA_ESTUDIANTE pe ON pe.id = i.estudiante_id
        INNER JOIN PERSONA p ON p.id = pe.persona_id
        LEFT JOIN PARROQUIA parr ON parr.id = pe.parroquia_nacimiento_id
        INNER JOIN PAIS pais ON pais.id = pe.pais_nacimiento_id
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
                        DateTime? fn = lector.IsDBNull(lector.GetOrdinal("FechaNacimiento")) ? null : lector.GetDateTime("FechaNacimiento");
                        lista.Add(new FilaSazeMatriculaDto
                        {
                            Numero = nro++,
                            Cedula = lector.GetString("Cedula"),
                            CedulaEscolar = lector.GetString("CedulaEscolar"),
                            Nombres = $"{lector.GetString("Nombre1")} {lector.GetString("Nombre2")}".Trim(),
                            Apellidos = $"{lector.GetString("Apellido1")} {lector.GetString("Apellido2")}".Trim(),
                            Sexo = lector.GetString("Sexo"),
                            FechaNacimiento = fn,
                            Edad = lector.IsDBNull(lector.GetOrdinal("Edad")) ? 0 : lector.GetInt32("Edad"),
                            LugarNacimiento = lector.GetString("LugarNacimiento"),
                            TipoIngreso = lector.GetString("TipoIngreso")
                        });
                    }
                }
            }

            return lista;
        }

        public List<FilaSazeRendimientoDto> ObtenerSazeRendimiento(int gradoSeccionId, int periodoId)
        {
            List<FilaSazeRendimientoDto> lista = new List<FilaSazeRendimientoDto>();

            const string consulta = @"
        SELECT m.nombre AS Materia,
               CONCAT_WS(' ', p.nombre_1, p.apellido_1) AS Docente,
               COUNT(DISTINCT i.id) AS Inscritos,
               COUNT(DISTINCT npi.id) AS Evaluados,
               SUM(CASE WHEN npi.nota >= 10 THEN 1 ELSE 0 END) AS Aprobados,
               SUM(CASE WHEN npi.nota < 10 AND npi.nota IS NOT NULL THEN 1 ELSE 0 END) AS Aplazados
        FROM MATERIA_PROFESOR_PERIODO mpp
        INNER JOIN GRADO_MATERIA gm ON gm.id = mpp.grado_materia_id
        INNER JOIN MATERIA m ON m.id = gm.materia_id
        INNER JOIN MATERIA_PROFESOR mp ON mp.id = mpp.materia_profesor_id
        INNER JOIN PROFESOR prof ON prof.id = mp.profesor_id
        INNER JOIN PERSONA p ON p.id = prof.persona_id
        INNER JOIN INSCRIPCION i ON i.grado_seccion_id = mpp.grado_seccion_id AND i.periodo_id = mpp.periodo_id
        LEFT JOIN NOTA_PERIODO_INSCRIPCION npi ON npi.inscripcion_id = i.id AND npi.materia_profe_periodo_id = mpp.id
        WHERE mpp.grado_seccion_id = @gsId AND mpp.periodo_id = @perId
        GROUP BY m.id, m.nombre, p.id
        ORDER BY m.nombre;";

            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@gsId", gradoSeccionId);
                comando.Parameters.AddWithValue("@perId", periodoId);

                using (MySqlDataReader lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        lista.Add(new FilaSazeRendimientoDto
                        {
                            Materia = lector.GetString("Materia"),
                            Docente = lector.GetString("Docente"),
                            Inscritos = Convert.ToInt32(lector["Inscritos"]),
                            Evaluados = Convert.ToInt32(lector["Evaluados"]),
                            Aprobados = Convert.ToInt32(lector["Aprobados"]),
                            Aplazados = Convert.ToInt32(lector["Aplazados"])
                        });
                    }
                }
            }

            return lista;
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

        public List<FilaNotaCertificadaDto> ObtenerNotasCertificadas(int estudianteId, int periodoId)
        {
            List<FilaNotaCertificadaDto> lista = new List<FilaNotaCertificadaDto>();

            const string consulta = @"
        SELECT g.nombre AS Grado,
               m.nombre AS Materia,
               pa.nombre AS Periodo,
               npi.nota AS Definitiva
        FROM INSCRIPCION i
        INNER JOIN PERIODO_ACADEMICO pa ON pa.id = i.periodo_id
        INNER JOIN MATERIA_PROFESOR_PERIODO mpp ON mpp.grado_seccion_id = i.grado_seccion_id AND mpp.periodo_id = i.periodo_id
        INNER JOIN GRADO_MATERIA gm ON gm.id = mpp.grado_materia_id
        INNER JOIN GRADO g ON g.id = gm.grado_id
        INNER JOIN MATERIA m ON m.id = gm.materia_id
        LEFT JOIN NOTA_PERIODO_INSCRIPCION npi ON npi.inscripcion_id = i.id AND npi.materia_profe_periodo_id = mpp.id
        WHERE i.estudiante_id = @estId AND i.periodo_id = @perId
        ORDER BY g.id, m.nombre;";

            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@estId", estudianteId);
                comando.Parameters.AddWithValue("@perId", periodoId);

                using (MySqlDataReader lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        int? nota = lector.IsDBNull(lector.GetOrdinal("Definitiva")) ? null : lector.GetInt32("Definitiva");
                        lista.Add(new FilaNotaCertificadaDto
                        {
                            Grado = lector.GetString("Grado"),
                            Materia = lector.GetString("Materia"),
                            Periodo = lector.GetString("Periodo"),
                            NotaNumero = nota,
                            NotaLetras = ConvertirNotaEnLetras(nota)
                        });
                    }
                }
            }

            return lista;
        }

        private static string ConvertirNotaEnLetras(int? nota)
        {
            if (!nota.HasValue) return "PENDIENTE";
            return nota.Value switch
            {
                0 => "CERO CERO",
                1 => "CERO UNO",
                2 => "CERO DOS",
                3 => "CERO TRES",
                4 => "CERO CUATRO",
                5 => "CERO CINCO",
                6 => "CERO SEIS",
                7 => "CERO SIETE",
                8 => "CERO OCHO",
                9 => "CERO NUEVE",
                10 => "DIEZ",
                11 => "ONCE",
                12 => "DOCE",
                13 => "TRECE",
                14 => "CATORCE",
                15 => "QUINCE",
                16 => "DIECISÉIS",
                17 => "DIECISIETE",
                18 => "DIECIOCHO",
                19 => "DIECINUEVE",
                20 => "VEINTE",
                _ => nota.Value.ToString()
            };
        }
    }
}