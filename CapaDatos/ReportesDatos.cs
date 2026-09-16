using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using ClosedXML.Excel;
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

        public static string ConvertirNotaEnLetras(int? nota)
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
        // =========================================================================
        // CÁLCULO DEL PROMEDIO REAL Y EXPORTACIÓN A EXCEL DE NOTAS CERTIFICADAS
        // =========================================================================

        public static decimal CalcularPromedioGeneralReal(CertificacionEstudianteCompletaDto dto)
        {
            List<decimal> notasReales = new List<decimal>();

            void Recolectar(List<FilaMateriaPensumDto> lista)
            {
                foreach (var m in lista)
                {
                    if (m.NotaNumero.HasValue && m.NotaNumero.Value >= 0 && m.NotaNumero.Value <= 20)
                    {
                        notasReales.Add(m.NotaNumero.Value);
                    }
                }
            }

            Recolectar(dto.PrimerAno);
            Recolectar(dto.SegundoAno);
            Recolectar(dto.TercerAno);
            Recolectar(dto.CuartoAno);
            Recolectar(dto.QuintoAno);

            return notasReales.Count > 0 ? Math.Round(notasReales.Average(), 3) : 0.000m;
        }

        public CertificacionEstudianteCompletaDto? ObtenerCertificacionOficialCompleta(int estudianteId)
        {
            const string consultaEstudiante = @"
        SELECT p.nombre_1, p.nombre_2, p.apellido_1, p.apellido_2,
               CONCAT(p.nacionalidad, '-', IFNULL(p.cedula_identidad, 'S/C')) AS Cedula,
               p.fecha_nacimiento,
               pais.nombre AS PaisNac,
               IFNULL(e.nombre, 'CARABOBO') AS EstadoNac,
               IFNULL(m.nombre, 'VALENCIA') AS MunNac
        FROM PERSONA_ESTUDIANTE pe
        INNER JOIN PERSONA p ON p.id = pe.persona_id
        INNER JOIN PAIS pais ON pais.id = pe.pais_nacimiento_id
        LEFT JOIN PARROQUIA par ON par.id = pe.parroquia_nacimiento_id
        LEFT JOIN MUNICIPIO m ON m.id = par.municipio_id
        LEFT JOIN ESTADO e ON e.id = m.estado_id
        WHERE pe.id = @estId LIMIT 1;";

            CertificacionEstudianteCompletaDto dto = new CertificacionEstudianteCompletaDto();

            using (MySqlConnection conexion = _conexion.AbrirConexion())
            {
                using (MySqlCommand cmdEst = new MySqlCommand(consultaEstudiante, conexion))
                {
                    cmdEst.Parameters.AddWithValue("@estId", estudianteId);
                    using (MySqlDataReader lector = cmdEst.ExecuteReader())
                    {
                        if (!lector.Read()) return null;

                        dto.Nombres = $"{lector.GetString("nombre_1")} {lector.GetString("nombre_2")}".Trim().ToUpper();
                        dto.Apellidos = $"{lector.GetString("apellido_1")} {lector.GetString("apellido_2")}".Trim().ToUpper();
                        dto.Cedula = lector.GetString("Cedula");
                        dto.FechaNacimiento = lector.IsDBNull(lector.GetOrdinal("fecha_nacimiento")) ? null : lector.GetDateTime("fecha_nacimiento");
                        dto.PaisNacimiento = lector.GetString("PaisNac").ToUpper();
                        dto.EstadoNacimiento = lector.GetString("EstadoNac").ToUpper();
                        dto.MunicipioNacimiento = lector.GetString("MunNac").ToUpper();
                    }
                }

                const string consultaNotas = @"
            SELECT g.nombre AS Grado,
                   m.nombre AS Materia,
                   npi.nota AS Definitiva,
                   pa.nombre AS Periodo
            FROM INSCRIPCION i
            INNER JOIN GRADO_SECCION gs ON gs.id = i.grado_seccion_id
            INNER JOIN GRADO g ON g.id = gs.grado_id
            INNER JOIN PERIODO_ACADEMICO pa ON pa.id = i.periodo_id
            INNER JOIN MATERIA_PROFESOR_PERIODO mpp ON mpp.grado_seccion_id = gs.id AND mpp.periodo_id = pa.id
            INNER JOIN GRADO_MATERIA gm ON gm.id = mpp.grado_materia_id
            INNER JOIN MATERIA m ON m.id = gm.materia_id
            LEFT JOIN NOTA_PERIODO_INSCRIPCION npi ON npi.inscripcion_id = i.id AND npi.materia_profe_periodo_id = mpp.id
            WHERE i.estudiante_id = @estId
            ORDER BY g.id, m.nombre;";

                using (MySqlCommand cmdNotas = new MySqlCommand(consultaNotas, conexion))
                {
                    cmdNotas.Parameters.AddWithValue("@estId", estudianteId);
                    using (MySqlDataReader lector = cmdNotas.ExecuteReader())
                    {
                        while (lector.Read())
                        {
                            string grado = lector.GetString("Grado").ToUpper();
                            string materia = lector.GetString("Materia").ToUpper();
                            int? nota = lector.IsDBNull(lector.GetOrdinal("Definitiva")) ? null : lector.GetInt32("Definitiva");

                            var item = new FilaMateriaPensumDto
                            {
                                Materia = materia,
                                NotaNumero = nota,
                                NotaLetras = ConvertirNotaEnLetras(nota),
                                TipoEvaluacion = "F",
                                MesAno = "07 2026",
                                InstitucionNro = 1
                            };

                            if (grado.Contains("1")) dto.PrimerAno.Add(item);
                            else if (grado.Contains("2")) dto.SegundoAno.Add(item);
                            else if (grado.Contains("3")) dto.TercerAno.Add(item);
                            else if (grado.Contains("4")) dto.CuartoAno.Add(item);
                            else if (grado.Contains("5")) dto.QuintoAno.Add(item);
                        }
                    }
                }

                LlenarPensumOficialBase(dto);
                dto.PromedioGeneral = CalcularPromedioGeneralReal(dto);
            }

            return dto;
        }


        private static void LlenarPensumOficialBase(CertificacionEstudianteCompletaDto dto)
        {
            AsegurarMaterias(dto.PrimerAno, new[] { "CASTELLANO", "INGLÉS Y OTRAS LENGUAS EXTRANJERAS", "MATEMÁTICAS", "EDUCACIÓN FÍSICA", "ARTE Y PATRIMONIO", "CIENCIAS NATURALES", "GEOGRAFÍA, HISTORIA Y CIUDADANÍA" });
            AsegurarMaterias(dto.SegundoAno, new[] { "CASTELLANO", "INGLÉS Y OTRAS LENGUAS EXTRANJERAS", "MATEMÁTICAS", "EDUCACIÓN FÍSICA", "ARTE Y PATRIMONIO", "CIENCIAS NATURALES", "GEOGRAFÍA, HISTORIA Y CIUDADANÍA" });
            AsegurarMaterias(dto.TercerAno, new[] { "CASTELLANO", "INGLÉS Y OTRAS LENGUAS EXTRANJERAS", "MATEMÁTICAS", "EDUCACIÓN FÍSICA", "FÍSICA", "QUÍMICA", "BIOLOGÍA", "GEOGRAFÍA, HISTORIA Y CIUDADANÍA" });
            AsegurarMaterias(dto.CuartoAno, new[] { "CASTELLANO", "INGLÉS Y OTRAS LENGUAS EXTRANJERAS", "MATEMÁTICAS", "EDUCACIÓN FÍSICA", "FÍSICA", "QUÍMICA", "BIOLOGÍA", "GEOGRAFÍA, HISTORIA Y CIUDADANÍA", "FORMACIÓN PARA LA SOBERANÍA NACIONAL" });
            AsegurarMaterias(dto.QuintoAno, new[] { "CASTELLANO", "INGLÉS Y OTRAS LENGUAS EXTRANJERAS", "MATEMÁTICAS", "EDUCACIÓN FÍSICA", "FÍSICA", "QUÍMICA", "BIOLOGÍA", "CIENCIAS DE LA TIERRA", "GEOGRAFÍA, HISTORIA Y CIUDADANÍA", "FORMACIÓN PARA LA SOBERANÍA NACIONAL" });
        }

        private static void AsegurarMaterias(List<FilaMateriaPensumDto> lista, string[] pensum)
        {
            foreach (var m in pensum)
            {
                if (!lista.Exists(x => x.Materia == m))
                {
                    lista.Add(new FilaMateriaPensumDto { Materia = m, NotaNumero = null, NotaLetras = "--", TipoEvaluacion = "F", MesAno = "07 2026", InstitucionNro = 1 });
                }
            }
        }

        public List<EstadisticaAnoSazeDto> ObtenerEstadisticasSazeMatriculaPorEdades(int periodoId)
        {
            var rangosPorGrado = new Dictionary<int, (int Min, int Max)>
            {
                { 1, (11, 16) },
                { 2, (11, 17) },
                { 3, (11, 18) },
                { 4, (11, 18) },
                { 5, (11, 22) }
            };

            var nombresGrados = new Dictionary<int, string>
            {
                { 1, "PRIMER AÑO" },
                { 2, "SEGUNDO AÑO" },
                { 3, "TERCER AÑO" },
                { 4, "CUARTO AÑO" },
                { 5, "QUINTO AÑO" }
            };

            var resultado = new List<EstadisticaAnoSazeDto>();
            for (int g = 1; g <= 5; g++)
            {
                var dto = new EstadisticaAnoSazeDto
                {
                    GradoNro = g,
                    Grado = nombresGrados[g]
                };

                var (min, max) = rangosPorGrado[g];
                for (int edad = min; edad <= max; edad++)
                {
                    dto.DistribucionEdades[edad] = (0, 0);
                }

                resultado.Add(dto);
            }

            const string consulta = @"
                SELECT g.id AS GradoId,
                       p.sexo AS Sexo,
                       p.nacionalidad AS Nacionalidad,
                       TIMESTAMPDIFF(YEAR, p.fecha_nacimiento, CURDATE()) AS Edad,
                       IFNULL(pe.poblacion_indigena, 'No') AS Indigena,
                       IFNULL(pe.embarazada, 'No') AS Embarazada,
                       IF(IFNULL(pe.posee_discapacidad, 'No') = 'Si' OR IFNULL(s.atencion_especial, 'No') = 'Si', 'Si', 'No') AS Discapacidad
                FROM INSCRIPCION i
                INNER JOIN GRADO_SECCION gs ON gs.id = i.grado_seccion_id
                INNER JOIN GRADO g ON g.id = gs.grado_id
                INNER JOIN PERSONA_ESTUDIANTE pe ON pe.id = i.estudiante_id
                INNER JOIN PERSONA p ON p.id = pe.persona_id
                LEFT JOIN SALUD s ON s.id = pe.salud_id
                WHERE i.periodo_id = @perId AND pe.ESTADO = 'Activo';";

            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@perId", periodoId);
                using (MySqlDataReader lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        int gradoId = lector.GetInt32("GradoId");
                        if (gradoId < 1 || gradoId > 5) continue;

                        var dto = resultado.Find(x => x.GradoNro == gradoId);
                        if (dto == null) continue;

                        string sexo = lector.GetString("Sexo").ToUpper();
                        bool esVaron = sexo == "M";
                        string nac = lector.GetString("Nacionalidad").ToUpper();
                        int edad = lector.IsDBNull(lector.GetOrdinal("Edad")) ? 11 : lector.GetInt32("Edad");
                        string indigena = lector.GetString("Indigena");
                        string embarazada = lector.GetString("Embarazada");
                        string discapacidad = lector.GetString("Discapacidad");

                        if (esVaron) dto.TotalVarones++;
                        else dto.TotalHembras++;

                        var (min, max) = rangosPorGrado[gradoId];
                        int edadClave = Math.Clamp(edad, min, max);

                        if (dto.DistribucionEdades.ContainsKey(edadClave))
                        {
                            var (m, f) = dto.DistribucionEdades[edadClave];
                            dto.DistribucionEdades[edadClave] = esVaron ? (m + 1, f) : (m, f + 1);
                        }

                        if (nac == "V")
                        {
                            if (esVaron) dto.VenezolanosM++;
                            else dto.VenezolanosF++;
                        }
                        else
                        {
                            if (esVaron) dto.ExtranjerosM++;
                            else dto.ExtranjerosF++;
                        }

                        if (indigena == "Si")
                        {
                            if (esVaron) dto.IndigenasM++;
                            else dto.IndigenasF++;
                        }

                        if (discapacidad == "Si")
                        {
                            if (esVaron) dto.DiscapacidadM++;
                            else dto.DiscapacidadF++;
                        }

                        if (!esVaron && embarazada == "Si")
                        {
                            dto.EmbarazadasF++;
                        }
                    }
                }
            }

            return resultado;
        }

        public void ExportarSazeMatriculaAExcel(int periodoId, string periodoNombre, string rutaArchivo)
        {
            ConfiguracionPlantelDatos confDatos = new ConfiguracionPlantelDatos();
            ConfiguracionPlantel conf = confDatos.Obtener();
            List<EstadisticaAnoSazeDto> estadisticas = ObtenerEstadisticasSazeMatriculaPorEdades(periodoId);

            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("SAZE Matrícula Inicial");
                ws.ShowGridLines = true;

                // 1. CONFIGURACIÓN DE ANCHOS DE COLUMNAS FIJOS (Evita que se aplasten)
                ws.Column(1).Width = 26; // Columna de etiquetas (Distribución por edades, Indicadores)
                for (int c = 2; c <= 32; c++)
                {
                    ws.Column(c).Width = 8.0; // Ancho óptimo para cada subcolumna V y H
                }

                // 2. MEMBRETE Y DATOS INSTITUCIONALES
                ws.Cell("A1").Value = "REPÚBLICA BOLIVARIANA DE VENEZUELA";
                ws.Cell("A2").Value = "MINISTERIO DEL PODER POPULAR PARA LA EDUCACIÓN";
                ws.Cell("A3").Value = conf.Eponimo;
                ws.Cell("A4").Value = $"{conf.CodigoPlantel} | CÓDIGO PLAN: {conf.CodigoPlanEstudio} ({conf.DenominacionPlan})";
                ws.Cell("A5").Value = $"UBICACIÓN: {conf.Direccion} | AÑO ESCOLAR: {periodoNombre}";
                ws.Cell("A6").Value = "FORMATO OFICIAL SAZE - REGISTRO Y RESUMEN ESTADÍSTICO DE MATRÍCULA INICIAL POR EDAD Y GÉNERO";

                int maxColTotal = 28; // Ancho para 5to año (11 a 22 años)

                for (int r = 1; r <= 6; r++)
                {
                    var rMembrete = ws.Range(r, 1, r, maxColTotal);
                    rMembrete.Merge();
                    rMembrete.Style.Font.Bold = true;
                    rMembrete.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    rMembrete.Style.Font.FontSize = (r == 3 || r == 6) ? 12 : 9.5;
                    if (r == 3) rMembrete.Style.Font.FontColor = XLColor.FromHtml("#1E3A8A");
                }

                int filaActual = 8;

                // 3. TABLAS POR CADA AÑO (1° A 5° AÑO)
                foreach (var ano in estadisticas)
                {
                    int cantEdades = ano.DistribucionEdades.Count;
                    int colFinal = 1 + (cantEdades * 2) + 3; // Columna final exacta de este año

                    // Ajustar ancho especial para las 3 columnas de totales
                    ws.Column(colFinal - 2).Width = 9.5; // TOT. V
                    ws.Column(colFinal - 1).Width = 9.5; // TOT. H
                    ws.Column(colFinal).Width = 10.5;    // TOTAL GENERAL

                    // Título del Año
                    var rangoTit = ws.Range(filaActual, 1, filaActual, colFinal);
                    rangoTit.Merge();
                    rangoTit.Value = $"ESTADÍSTICA DE MATRÍCULA: {ano.Grado}";
                    rangoTit.Style.Font.Bold = true;
                    rangoTit.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
                    rangoTit.Style.Font.FontColor = XLColor.White;
                    rangoTit.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    rangoTit.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    ws.Row(filaActual).Height = 24;
                    filaActual++;

                    // Fila 1 de Encabezados (Edades combinadas de 2 en 2 columnas)
                    ws.Range(filaActual, 1, filaActual + 1, 1).Merge();
                    ws.Cell(filaActual, 1).Value = "DISTRIBUCIÓN POR EDAD";
                    ws.Cell(filaActual, 1).Style.Font.Bold = true;
                    ws.Cell(filaActual, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(filaActual, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    ws.Cell(filaActual, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");

                    int col = 2;
                    foreach (var edad in ano.DistribucionEdades.Keys)
                    {
                        var rEdad = ws.Range(filaActual, col, filaActual, col + 1);
                        rEdad.Merge();
                        rEdad.Value = $"{edad} AÑOS";
                        rEdad.Style.Font.Bold = true;
                        rEdad.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        rEdad.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        rEdad.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
                        col += 2;
                    }

                    // Totales por género
                    ws.Range(filaActual, col, filaActual + 1, col).Merge();
                    ws.Cell(filaActual, col).Value = "TOT. V";
                    ws.Cell(filaActual, col).Style.Font.Bold = true;
                    ws.Cell(filaActual, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
                    ws.Cell(filaActual, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(filaActual, col).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    ws.Range(filaActual, col + 1, filaActual + 1, col + 1).Merge();
                    ws.Cell(filaActual, col + 1).Value = "TOT. H";
                    ws.Cell(filaActual, col + 1).Style.Font.Bold = true;
                    ws.Cell(filaActual, col + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
                    ws.Cell(filaActual, col + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(filaActual, col + 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    ws.Range(filaActual, col + 2, filaActual + 1, col + 2).Merge();
                    ws.Cell(filaActual, col + 2).Value = "TOTAL";
                    ws.Cell(filaActual, col + 2).Style.Font.Bold = true;
                    ws.Cell(filaActual, col + 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF08A"); // Amarillo
                    ws.Cell(filaActual, col + 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(filaActual, col + 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    ws.Row(filaActual).Height = 20;
                    filaActual++;

                    // Fila 2 de Encabezados (Subcolumnas V y H)
                    col = 2;
                    for (int i = 0; i < cantEdades; i++)
                    {
                        ws.Cell(filaActual, col).Value = "V";
                        ws.Cell(filaActual, col).Style.Font.Bold = true;
                        ws.Cell(filaActual, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(filaActual, col).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");

                        ws.Cell(filaActual, col + 1).Value = "H";
                        ws.Cell(filaActual, col + 1).Style.Font.Bold = true;
                        ws.Cell(filaActual, col + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(filaActual, col + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                        col += 2;
                    }

                    ws.Row(filaActual).Height = 18;
                    filaActual++;

                    // Fila de Valores de Matrícula
                    ws.Cell(filaActual, 1).Value = "Nº de Estudiantes";
                    ws.Cell(filaActual, 1).Style.Font.Bold = true;
                    ws.Cell(filaActual, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    col = 2;
                    foreach (var (m, f) in ano.DistribucionEdades.Values)
                    {
                        ws.Cell(filaActual, col).Value = m;
                        ws.Cell(filaActual, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(filaActual, col).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                        ws.Cell(filaActual, col + 1).Value = f;
                        ws.Cell(filaActual, col + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        ws.Cell(filaActual, col + 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        col += 2;
                    }

                    ws.Cell(filaActual, col).Value = ano.TotalVarones;
                    ws.Cell(filaActual, col).Style.Font.Bold = true;
                    ws.Cell(filaActual, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(filaActual, col).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    ws.Cell(filaActual, col + 1).Value = ano.TotalHembras;
                    ws.Cell(filaActual, col + 1).Style.Font.Bold = true;
                    ws.Cell(filaActual, col + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(filaActual, col + 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    ws.Cell(filaActual, col + 2).Value = ano.TotalGeneral;
                    ws.Cell(filaActual, col + 2).Style.Font.Bold = true;
                    ws.Cell(filaActual, col + 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF08A");
                    ws.Cell(filaActual, col + 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(filaActual, col + 2).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                    ws.Row(filaActual).Height = 22;

                    // Bordes de la tabla de edades
                    var rangoTablaAno = ws.Range(filaActual - 2, 1, filaActual, colFinal);
                    rangoTablaAno.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    rangoTablaAno.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                    filaActual += 2;

                    // ================= SUBTABLA DE POBLACIONES ESPECIALES =================
                    // Dividir las columnas restantes proporcionalmente entre los 5 indicadores
                    int colsDisponibles = colFinal - 1;
                    int step = Math.Max(2, colsDisponibles / 5);

                    string[] titulosInd = { "VENEZOLANOS", "EXTRANJEROS", "POBLACIÓN INDÍGENAS", "CON DISCAPACIDAD (NEE)", "EMBARAZADAS (H)" };
                    string[] valoresInd = {
                $"V: {ano.VenezolanosM} | H: {ano.VenezolanosF} ({ano.VenezolanosM + ano.VenezolanosF})",
                $"V: {ano.ExtranjerosM} | H: {ano.ExtranjerosF} ({ano.ExtranjerosM + ano.ExtranjerosF})",
                $"V: {ano.IndigenasM} | H: {ano.IndigenasF} ({ano.IndigenasM + ano.IndigenasF})",
                $"V: {ano.DiscapacidadM} | H: {ano.DiscapacidadF} ({ano.DiscapacidadM + ano.DiscapacidadF})",
                $"{ano.EmbarazadasF}"
            };

                    // Encabezado de indicadores
                    ws.Cell(filaActual, 1).Value = "INDICADORES";
                    ws.Cell(filaActual, 1).Style.Font.Bold = true;
                    ws.Cell(filaActual, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
                    ws.Cell(filaActual, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    int cIni = 2;
                    for (int i = 0; i < 5; i++)
                    {
                        int cFin = (i == 4) ? colFinal : (cIni + step - 1);
                        var rInd = ws.Range(filaActual, cIni, filaActual, cFin);
                        rInd.Merge();
                        rInd.Value = titulosInd[i];
                        rInd.Style.Font.Bold = true;
                        rInd.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
                        rInd.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        cIni = cFin + 1;
                    }

                    filaActual++;

                    // Valores de indicadores
                    ws.Cell(filaActual, 1).Value = "Desglose (V / H / Total)";
                    ws.Cell(filaActual, 1).Style.Font.Bold = true;

                    cIni = 2;
                    for (int i = 0; i < 5; i++)
                    {
                        int cFin = (i == 4) ? colFinal : (cIni + step - 1);
                        var rVal = ws.Range(filaActual, cIni, filaActual, cFin);
                        rVal.Merge();
                        rVal.Value = valoresInd[i];
                        rVal.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        rVal.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                        cIni = cFin + 1;
                    }

                    var rangoIndBordes = ws.Range(filaActual - 1, 1, filaActual, colFinal);
                    rangoIndBordes.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    rangoIndBordes.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                    filaActual += 3;
                }

                // ================= 4. CONSOLIDADO GENERAL DEL PLANTEL =================
                var rTotalTit = ws.Range(filaActual, 1, filaActual, maxColTotal);
                rTotalTit.Merge();
                rTotalTit.Value = "CONSOLIDADO GENERAL INSTITUCIONAL (1° A 5° AÑO)";
                rTotalTit.Style.Font.Bold = true;
                rTotalTit.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F172A");
                rTotalTit.Style.Font.FontColor = XLColor.White;
                rTotalTit.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Row(filaActual).Height = 24;
                filaActual++;

                string[] headersTotales = { "TOTAL VARONES (M)", "TOTAL HEMBRAS (F)", "TOTAL GENERAL PLANTEL", "POBLACIÓN INDÍGENAS", "CON DISCAPACIDAD", "EMBARAZADAS" };
                int[] valoresTotales = {
            estadisticas.Sum(x => x.TotalVarones),
            estadisticas.Sum(x => x.TotalHembras),
            estadisticas.Sum(x => x.TotalGeneral),
            estadisticas.Sum(x => x.IndigenasM + x.IndigenasF),
            estadisticas.Sum(x => x.DiscapacidadM + x.DiscapacidadF),
            estadisticas.Sum(x => x.EmbarazadasF)
        };

                int stepTot = maxColTotal / 6;
                int colTIni = 1;

                for (int i = 0; i < 6; i++)
                {
                    int colTFin = (i == 5) ? maxColTotal : (colTIni + stepTot - 1);

                    // Cabecera
                    var rH = ws.Range(filaActual, colTIni, filaActual, colTFin);
                    rH.Merge();
                    rH.Value = headersTotales[i];
                    rH.Style.Font.Bold = true;
                    rH.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
                    rH.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    // Valor
                    var rV = ws.Range(filaActual + 1, colTIni, filaActual + 1, colTFin);
                    rV.Merge();
                    rV.Value = valoresTotales[i];
                    rV.Style.Font.Bold = true;
                    rV.Style.Font.FontSize = 12;
                    rV.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    if (i == 2) rV.Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF08A"); // Amarillo para Total General

                    colTIni = colTFin + 1;
                }

                var rangoTotBordes = ws.Range(filaActual, 1, filaActual + 1, maxColTotal);
                rangoTotBordes.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                rangoTotBordes.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                // Guardar archivo Excel
                wb.SaveAs(rutaArchivo);
            }
        }
        public void ExportarConstanciaAExcel(ConstanciaEstudioDto datos, ConfiguracionPlantel conf, string titulo, bool esConducta, string rutaArchivo, string estiloCedula)
        {
            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("Constancia");
                ws.ShowGridLines = false;

                ws.Column(1).Width = 5;
                ws.Column(2).Width = 75;
                ws.Column(3).Width = 5;

                // Membrete
                ws.Cell("B2").Value = "REPÚBLICA BOLIVARIANA DE VENEZUELA";
                ws.Cell("B3").Value = "MINISTERIO DEL PODER POPULAR PARA LA EDUCACIÓN";
                ws.Cell("B4").Value = conf.Eponimo;
                ws.Cell("B5").Value = $"{conf.CodigoPlantel} | {conf.Direccion}";

                for (int r = 2; r <= 5; r++)
                {
                    ws.Cell(r, 2).Style.Font.Bold = true;
                    ws.Cell(r, 2).Style.Font.FontSize = (r == 4) ? 12 : 9.5;
                    ws.Cell(r, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                // Título
                ws.Cell("B8").Value = titulo;
                ws.Cell("B8").Style.Font.Bold = true;
                ws.Cell("B8").Style.Font.FontSize = 15;
                ws.Cell("B8").Style.Font.FontColor = XLColor.FromHtml("#1E3A8A");
                ws.Cell("B8").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                string cedulaFormateada = FormatearCedula(datos.Cedula, estiloCedula);

                // Cuerpo del documento (Texto editable)
                string cuerpo = esConducta
                    ? $"Quien suscribe, la Dirección de la institución {conf.Eponimo}, hace constar por medio de la presente que el/la estudiante {datos.EstudianteNombreCompleto}, titular de la Cédula de Identidad Nº {cedulaFormateada}, cursante del {datos.Grado}, Sección \"{datos.Seccion}\", durante el año escolar {datos.Periodo}, ha demostrado una EXCELENTE CONDUCTA, acatando las normas de convivencia escolar y demostrando respeto y colaboración."
                    : $"Quien suscribe, la Dirección de la institución {conf.Eponimo}, hace constar por medio de la presente que el/la estudiante {datos.EstudianteNombreCompleto}, titular de la Cédula de Identidad Nº {cedulaFormateada} (Cédula Escolar Nº {datos.CedulaEscolar}), se encuentra debidamente inscrito(a) en este plantel cursando el {datos.Grado}, Sección \"{datos.Seccion}\" de Educación {datos.NivelAcademico}, durante el Año Escolar {datos.Periodo}.";

                var cCuerpo = ws.Cell("B11");
                cCuerpo.Value = cuerpo;
                cCuerpo.Style.Alignment.WrapText = true;
                cCuerpo.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                cCuerpo.Style.Font.FontSize = 11;
                ws.Row(11).Height = 85;

                string fechaHoy = DateTime.Now.ToString("dd 'días del mes de' MMMM 'de' yyyy", new CultureInfo("es-ES"));
                var cFecha = ws.Cell("B14");
                cFecha.Value = $"Constancia que se expide a petición de la parte interesada, en la ciudad de {conf.Municipio}, a los {fechaHoy}.";
                cFecha.Style.Font.FontSize = 10.5;
                cFecha.Style.Alignment.WrapText = true;

                // Firmas
                ws.Cell("B19").Value = "_________________________________                     _________________________________";
                ws.Cell("B20").Value = $"{conf.DirectorNombre}                                         Control de Estudios y Evaluación";
                ws.Cell("B21").Value = $"Director(a) - C.I. {conf.DirectorCedula}                                              Sello del Plantel";

                for (int r = 19; r <= 21; r++)
                {
                    ws.Cell(r, 2).Style.Font.Bold = true;
                    ws.Cell(r, 2).Style.Font.FontSize = 9.5;
                    ws.Cell(r, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }

                wb.SaveAs(rutaArchivo);
            }
        }

        public void ExportarBoletaAExcel(ConstanciaEstudioDto est, List<FilaBoletaDto> notas, ConfiguracionPlantel conf, string rutaArchivo, string estiloCedula)
        {
            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("Boletín de Calificaciones");
                ws.ShowGridLines = true;

                ws.Column(1).Width = 32; // Asignatura
                ws.Column(2).Width = 28; // Docente
                ws.Column(3).Width = 14; // Lapso 1
                ws.Column(4).Width = 14; // Lapso 2
                ws.Column(5).Width = 14; // Lapso 3
                ws.Column(6).Width = 16; // Definitiva

                // Encabezado
                ws.Range("A1:F1").Merge().Value = conf.Eponimo;
                ws.Range("A2:F2").Merge().Value = "BOLETÍN INFORMATIVO DE CALIFICACIONES";
                ws.Range("A1:F2").Style.Font.Bold = true;
                ws.Range("A1:F2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell("A1").Style.Font.FontSize = 13;
                ws.Cell("A2").Style.Font.FontSize = 11;
                ws.Cell("A2").Style.Font.FontColor = XLColor.FromHtml("#1E3A8A");

                // Datos del Estudiante
                string cedula = FormatearCedula(est.Cedula, estiloCedula);
                ws.Cell("A4").Value = $"Estudiante: {est.EstudianteNombreCompleto}";
                ws.Cell("D4").Value = $"Cédula: {cedula}";
                ws.Cell("A5").Value = $"Año y Sección: {est.Grado} \"{est.Seccion}\"";
                ws.Cell("D5").Value = $"Año Escolar: {est.Periodo}";
                ws.Range("A4:F5").Style.Font.Bold = true;

                // Tabla de Notas
                int r = 7;
                string[] headers = { "ASIGNATURA", "DOCENTE", "1ER LAPSO", "2DO LAPSO", "3ER LAPSO", "DEFINITIVA" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var c = ws.Cell(r, i + 1);
                    c.Value = headers[i];
                    c.Style.Font.Bold = true;
                    c.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
                    c.Style.Font.FontColor = XLColor.White;
                    c.Style.Alignment.Horizontal = (i < 2) ? XLAlignmentHorizontalValues.Left : XLAlignmentHorizontalValues.Center;
                }
                r++;

                foreach (var n in notas)
                {
                    ws.Cell(r, 1).Value = n.Materia;
                    ws.Cell(r, 2).Value = n.Docente;
                    ws.Cell(r, 3).Value = n.NotaLapso1.HasValue ? n.NotaLapso1.Value.ToString("D2") : "--";
                    ws.Cell(r, 4).Value = n.NotaLapso2.HasValue ? n.NotaLapso2.Value.ToString("D2") : "--";
                    ws.Cell(r, 5).Value = n.NotaLapso3.HasValue ? n.NotaLapso3.Value.ToString("D2") : "--";
                    ws.Cell(r, 6).Value = n.NotaDefinitiva.HasValue ? n.NotaDefinitiva.Value.ToString("D2") : "--";

                    for (int col = 3; col <= 6; col++)
                        ws.Cell(r, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(r, 6).Style.Font.Bold = true;
                    ws.Cell(r, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF08A");
                    r++;
                }

                var rangoTabla = ws.Range(7, 1, r - 1, 6);
                rangoTabla.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                rangoTabla.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                // Firmas
                r += 3;
                ws.Range(r, 1, r, 3).Merge().Value = "____________________________________";
                ws.Range(r, 4, r, 6).Merge().Value = "____________________________________";
                ws.Range(r + 1, 1, r + 1, 3).Merge().Value = $"Prof(a). {conf.DirectorNombre}\nDirector(a)";
                ws.Range(r + 1, 4, r + 1, 6).Merge().Value = "Control de Estudios y Evaluación\nSello del Plantel";
                ws.Range(r, 1, r + 1, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Range(r + 1, 1, r + 1, 6).Style.Font.Bold = true;

                wb.SaveAs(rutaArchivo);
            }
        }
        public void ExportarNominaAExcel(List<FilaNominaSeccionDto> lista, string gradoSeccion, string periodo, ConfiguracionPlantel conf, string rutaArchivo, string estiloCedula)
        {
            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("Nómina de Matrícula");
                ws.ShowGridLines = true;

                ws.Column(1).Width = 8;  // Nº
                ws.Column(2).Width = 16; // Cédula
                ws.Column(3).Width = 35; // Estudiante
                ws.Column(4).Width = 8;  // Sexo
                ws.Column(5).Width = 32; // Representante
                ws.Column(6).Width = 18; // Teléfono

                // Encabezado
                ws.Range("A1:F1").Merge().Value = conf.Eponimo;
                ws.Range("A2:F2").Merge().Value = $"NÓMINA DE MATRÍCULA - {gradoSeccion.ToUpper()} | AÑO ESCOLAR {periodo}";
                ws.Range("A1:F2").Style.Font.Bold = true;
                ws.Range("A1:F2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell("A1").Style.Font.FontSize = 13;
                ws.Cell("A2").Style.Font.FontSize = 11;
                ws.Cell("A2").Style.Font.FontColor = XLColor.FromHtml("#1E3A8A");

                int r = 4;
                string[] headers = { "Nº", "CÉDULA", "APELLIDOS Y NOMBRES", "SEXO", "REPRESENTANTE LEGAL", "TELÉFONO" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var c = ws.Cell(r, i + 1);
                    c.Value = headers[i];
                    c.Style.Font.Bold = true;
                    c.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
                    c.Style.Font.FontColor = XLColor.White;
                    c.Style.Alignment.Horizontal = (i == 2 || i == 4) ? XLAlignmentHorizontalValues.Left : XLAlignmentHorizontalValues.Center;
                }
                r++;

                foreach (var item in lista)
                {
                    ws.Cell(r, 1).Value = item.Numero;
                    ws.Cell(r, 2).Value = FormatearCedula(item.Cedula, estiloCedula);
                    ws.Cell(r, 3).Value = item.Estudiante;
                    ws.Cell(r, 4).Value = item.Sexo;
                    ws.Cell(r, 5).Value = item.Representante;
                    ws.Cell(r, 6).Value = item.TelefonoRepresentante;

                    ws.Cell(r, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(r, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(r, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(r, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    r++;
                }

                var rangoTabla = ws.Range(4, 1, r - 1, 6);
                rangoTabla.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                rangoTabla.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                // Resumen final
                ws.Cell(r + 1, 1).Value = $"Total Estudiantes: {lista.Count}   |   Varones (M): {lista.Count(x => x.Sexo == "M")}   |   Hembras (F): {lista.Count(x => x.Sexo == "F")}";
                ws.Range(r + 1, 1, r + 1, 6).Merge().Style.Font.Bold = true;

                wb.SaveAs(rutaArchivo);
            }
        }

        public void ExportarNotasCertificadasAExcel(CertificacionEstudianteCompletaDto est, ConfiguracionPlantel conf, string rutaArchivo, string estiloCedula)
        {
            // Asegurar cálculo del promedio real actualizado
            est.PromedioGeneral = CalcularPromedioGeneralReal(est);

            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("Notas Certificadas");
                ws.ShowGridLines = true;

                // Anchos de columnas para las 2 tablas paralelas (1° a 5° y Complementarias)
                ws.Column(1).Width = 25; // Materia Izq
                ws.Column(2).Width = 5;  // Nº
                ws.Column(3).Width = 14; // Letras
                ws.Column(4).Width = 6;  // T-E
                ws.Column(5).Width = 11; // Fecha
                ws.Column(6).Width = 5;  // Inst
                ws.Column(7).Width = 2.5;// Separador central
                ws.Column(8).Width = 25; // Materia Der
                ws.Column(9).Width = 5;  // Nº
                ws.Column(10).Width = 14;// Letras
                ws.Column(11).Width = 6; // T-E
                ws.Column(12).Width = 11;// Fecha
                ws.Column(13).Width = 5; // Inst

                // ================= I. ENCABEZADO OFICIAL =================
                ws.Range("A1:M1").Merge().Value = "REPÚBLICA BOLIVARIANA DE VENEZUELA - MINISTERIO DEL PODER POPULAR PARA LA EDUCACIÓN";
                ws.Range("A2:M2").Merge().Value = $"CERTIFICACIÓN DE CALIFICACIONES EMG - {conf.DenominacionPlan} (PLAN {conf.CodigoPlanEstudio})";
                ws.Range("A1:M2").Style.Font.Bold = true;
                ws.Range("A1:M2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // ================= II Y III. DATOS PLANTEL Y ESTUDIANTE =================
                ws.Range("A4:M4").Merge().Value = $"II. DATOS DEL PLANTEL: {conf.Eponimo} | CÓDIGO: {conf.CodigoPlantel} | {conf.Municipio}, {conf.EntidadFederal} | TELÉFONO: {conf.Telefono}";
                ws.Range("A4:M4").Style.Font.Bold = true;
                ws.Range("A4:M4").Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");

                string cedula = FormatearCedula(est.Cedula, estiloCedula);
                string fn = est.FechaNacimiento.HasValue ? est.FechaNacimiento.Value.ToString("dd/MM/yyyy") : "S/F";
                ws.Range("A5:M5").Merge().Value = $"III. ESTUDIANTE: {est.Apellidos}, {est.Nombres} | CÉDULA: {cedula} | F. NAC: {fn} | LUGAR: {est.MunicipioNacimiento}, {est.EstadoNacimiento}";
                ws.Range("A5:M5").Style.Font.Bold = true;
                ws.Range("A5:M5").Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");

                // ================= IV. PLANTELES CURSADOS =================
                ws.Range("A6:M6").Merge().Value = $"IV. INSTITUCIÓN EDUCATIVA: (1) {conf.Eponimo} - {conf.Municipio} (EDO. {conf.EntidadFederal})";
                ws.Range("A6:M6").Style.Font.Bold = true;
                ws.Range("A6:M6").Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");

                // Función interna para dibujar cada año
                void DibujarBloqueAno(int rIni, int cIni, string titulo, List<FilaMateriaPensumDto> materias)
                {
                    ws.Range(rIni, cIni, rIni, cIni + 5).Merge().Value = titulo;
                    ws.Range(rIni, cIni, rIni, cIni + 5).Style.Font.Bold = true;
                    ws.Range(rIni, cIni, rIni, cIni + 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
                    ws.Range(rIni, cIni, rIni, cIni + 5).Style.Font.FontColor = XLColor.White;
                    ws.Range(rIni, cIni, rIni, cIni + 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    string[] subH = { "ÁREA DE FORMACIÓN", "Nº", "LETRAS", "T-E", "FECHA", "I" };
                    for (int i = 0; i < subH.Length; i++)
                    {
                        var cel = ws.Cell(rIni + 1, cIni + i);
                        cel.Value = subH[i];
                        cel.Style.Font.Bold = true;
                        cel.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
                        cel.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        cel.Style.Font.FontSize = 8;
                    }

                    int f = rIni + 2;
                    foreach (var m in materias)
                    {
                        ws.Cell(f, cIni).Value = m.Materia;
                        ws.Cell(f, cIni + 1).Value = m.NotaNumero.HasValue ? m.NotaNumero.Value.ToString("D2") : "--";
                        ws.Cell(f, cIni + 2).Value = m.NotaLetras;
                        ws.Cell(f, cIni + 3).Value = m.TipoEvaluacion;
                        ws.Cell(f, cIni + 4).Value = m.MesAno;
                        ws.Cell(f, cIni + 5).Value = m.InstitucionNro;

                        for (int col = 1; col <= 5; col++)
                            ws.Cell(f, cIni + col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        ws.Row(f).Style.Font.FontSize = 8;
                        f++;
                    }

                    var rangoBloque = ws.Range(rIni, cIni, f - 1, cIni + 5);
                    rangoBloque.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    rangoBloque.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                }

                // Fila 1: 1er Año (Izq) | 2do Año (Der)
                DibujarBloqueAno(8, 1, "PRIMER AÑO", est.PrimerAno);
                DibujarBloqueAno(8, 8, "SEGUNDO AÑO", est.SegundoAno);

                // Fila 2: 3er Año (Izq) | 4to Año (Der)
                DibujarBloqueAno(18, 1, "TERCER AÑO", est.TercerAno);
                DibujarBloqueAno(18, 8, "CUARTO AÑO", est.CuartoAno);

                // Fila 3: 5to Año (Izq)
                DibujarBloqueAno(29, 1, "QUINTO AÑO", est.QuintoAno);

                // ================= ÁREAS COMPLEMENTARIAS EN EXCEL (Lado Derecho de 5to Año) =================
                int rComp = 29;
                int cComp = 8;
                ws.Range(rComp, cComp, rComp, cComp + 5).Merge().Value = "ÁREAS DE FORMACIÓN COMPLEMENTARIAS";
                ws.Range(rComp, cComp, rComp, cComp + 5).Style.Font.Bold = true;
                ws.Range(rComp, cComp, rComp, cComp + 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
                ws.Range(rComp, cComp, rComp, cComp + 5).Style.Font.FontColor = XLColor.White;
                ws.Range(rComp, cComp, rComp, cComp + 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Subtítulos
                ws.Range(rComp + 1, cComp, rComp + 1, cComp + 2).Merge().Value = "ÁREA DE FORMACIÓN";
                ws.Cell(rComp + 1, cComp + 3).Value = "AÑO";
                ws.Range(rComp + 1, cComp + 4, rComp + 1, cComp + 5).Merge().Value = "LITERAL / GRUPO";
                ws.Range(rComp + 1, cComp, rComp + 1, cComp + 5).Style.Font.Bold = true;
                ws.Range(rComp + 1, cComp, rComp + 1, cComp + 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
                ws.Range(rComp + 1, cComp, rComp + 1, cComp + 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Range(rComp + 1, cComp, rComp + 1, cComp + 5).Style.Font.FontSize = 8;

                int fC = rComp + 2;
                // Orientación y Convivencia (1° a 5°)
                for (int a = 1; a <= 5; a++)
                {
                    ws.Range(fC, cComp, fC, cComp + 2).Merge().Value = "ORIENTACIÓN Y CONVIVENCIA";
                    ws.Cell(fC, cComp + 3).Value = $"{a}°";
                    ws.Range(fC, cComp + 4, fC, cComp + 5).Merge().Value = "A";

                    ws.Cell(fC, cComp + 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Range(fC, cComp + 4, fC, cComp + 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Row(fC).Style.Font.FontSize = 7.5;
                    fC++;
                }

                // Grupos Estables (1° a 5°)
                for (int a = 1; a <= 5; a++)
                {
                    ws.Range(fC, cComp, fC, cComp + 2).Merge().Value = "PARTICIPACIÓN EN G.C.R.P.";
                    ws.Cell(fC, cComp + 3).Value = $"{a}°";
                    ws.Range(fC, cComp + 4, fC, cComp + 5).Merge().Value = "A (PROTOCOLO)";

                    ws.Cell(fC, cComp + 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Range(fC, cComp + 4, fC, cComp + 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Row(fC).Style.Font.FontSize = 7.5;
                    fC++;
                }

                var rangoComp = ws.Range(rComp, cComp, fC - 1, cComp + 5);
                rangoComp.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rangoComp.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                // ================= VI. OBSERVACIONES CON PROMEDIO REAL =================
                ws.Range("A42:M42").Merge().Value = $"VI. OBSERVACIONES:   PROMEDIO GENERAL: {est.PromedioGeneral:N3}";
                ws.Range("A42:M42").Style.Font.Bold = true;
                ws.Range("A42:M42").Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF08A"); // Amarillo
                ws.Range("A42:M42").Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

                // ================= VII Y VIII. FIRMAS Y VALOR FISCAL =================
                ws.Range("A45:F45").Merge().Value = $"_________________________________\nProf(a). {conf.DirectorNombre}\nDirector(a) del Plantel\n(Para efectos de su Validez Nacional)";
                ws.Range("H45:M45").Merge().Value = "_________________________________\nDirector(a) de la Calidad Educativa\nSello CDCEE\n(Para efectos de su Validez Internacional)";
                ws.Range("A45:M45").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Range("A45:M45").Style.Font.Bold = true;

                ws.Range("A48:M48").Merge().Value = "VALOR FISCAL: Para su validez legal y de acuerdo a la Ley de Timbre Fiscal al dorso de este documento se le debe colocar tres décimas de la Unidad Tributaria (0,3 U.T.)";
                ws.Range("A48:M48").Style.Font.FontSize = 8;
                ws.Range("A48:M48").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                wb.SaveAs(rutaArchivo);
            }
        }

        public void ExportarSazeRendimientoAExcel(List<FilaSazeRendimientoDto> lista, string gradoSeccion, string periodo, ConfiguracionPlantel conf, string rutaArchivo)
        {
            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("SAZE Rendimiento");
                ws.ShowGridLines = true;

                ws.Column(1).Width = 28; // Asignatura
                ws.Column(2).Width = 26; // Docente
                ws.Column(3).Width = 14; // Matrícula
                ws.Column(4).Width = 14; // Evaluados
                ws.Column(5).Width = 14; // Aprobados
                ws.Column(6).Width = 14; // Aplazados
                ws.Column(7).Width = 16; // % Aprobación

                // Encabezado
                ws.Range("A1:G1").Merge().Value = conf.Eponimo;
                ws.Range("A2:G2").Merge().Value = $"FORMATO OFICIAL SAZE - RESUMEN DE RENDIMIENTO ESCOLAR ({gradoSeccion.ToUpper()}) - AÑO {periodo}";
                ws.Range("A1:G2").Style.Font.Bold = true;
                ws.Range("A1:G2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell("A1").Style.Font.FontSize = 13;
                ws.Cell("A2").Style.Font.FontSize = 11;
                ws.Cell("A2").Style.Font.FontColor = XLColor.FromHtml("#1E3A8A");

                int r = 4;
                string[] headers = { "ASIGNATURA", "DOCENTE", "MATRÍCULA", "EVALUADOS", "APROBADOS", "APLAZADOS", "% APROBACIÓN" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var c = ws.Cell(r, i + 1);
                    c.Value = headers[i];
                    c.Style.Font.Bold = true;
                    c.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
                    c.Style.Font.FontColor = XLColor.White;
                    c.Style.Alignment.Horizontal = (i < 2) ? XLAlignmentHorizontalValues.Left : XLAlignmentHorizontalValues.Center;
                }
                r++;

                foreach (var item in lista)
                {
                    ws.Cell(r, 1).Value = item.Materia;
                    ws.Cell(r, 2).Value = item.Docente;
                    ws.Cell(r, 3).Value = item.Inscritos;
                    ws.Cell(r, 4).Value = item.Evaluados;
                    ws.Cell(r, 5).Value = item.Aprobados;
                    ws.Cell(r, 6).Value = item.Aplazados;
                    ws.Cell(r, 7).Value = $"{item.PorcentajeAprobados}%";

                    for (int col = 3; col <= 7; col++)
                        ws.Cell(r, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(r, 7).Style.Font.Bold = true;
                    r++;
                }

                var rangoTabla = ws.Range(4, 1, r - 1, 7);
                rangoTabla.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                rangoTabla.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                wb.SaveAs(rutaArchivo);
            }
        }
        private string FormatearCedula(string? cedula, string estiloCedula)
        {
            if (string.IsNullOrWhiteSpace(cedula))
                return string.Empty;

            // Extraer prefijo (V/E/J/G) y dígitos limpios
            string prefijo = "V-";
            string digitos = new string(cedula.Where(char.IsDigit).ToArray());

            if (cedula.StartsWith("E", StringComparison.OrdinalIgnoreCase))
                prefijo = "E-";
            else if (cedula.StartsWith("J", StringComparison.OrdinalIgnoreCase))
                prefijo = "J-";
            else if (cedula.StartsWith("G", StringComparison.OrdinalIgnoreCase))
                prefijo = "G-";

            if (!long.TryParse(digitos, out long numero))
                return cedula;

            // Aplicar formato según la preferencia
            switch (estiloCedula?.ToUpper())
            {
                case "SIN_PUNTOS":
                    return $"{prefijo}{digitos}";

                case "SOLO_NUMEROS":
                    return string.Format(new System.Globalization.CultureInfo("es-VE"), "{0:N0}", numero).Replace(',', '.');

                case "CON_PUNTOS":
                default:
                    string numeroConPuntos = string.Format(new System.Globalization.CultureInfo("es-VE"), "{0:N0}", numero).Replace(',', '.');
                    return $"{prefijo}{numeroConPuntos}";
            }
        }
        public void ExportarNominaEvaluacionContinuaDocenteAExcel(
    int materiaProfePeriodoId,
    string gradoNombre,
    string seccionNombre,
    string materiaNombre,
    string docenteNombre,
    string periodoNombre,
    string lapsoNombre,
    string[] nombresEvaluaciones,
    List<FilaPlanillaNotasDto> estudiantes,
    string rutaArchivo)
        {
            ConfiguracionPlantelDatos confDatos = new();
            ConfiguracionPlantel conf = confDatos.Obtener();

            using (var wb = new XLWorkbook())
            {
                var ws = wb.Worksheets.Add("Evaluación Continua");
                ws.ShowGridLines = true;

                // Configuración de anchos de columna
                ws.Column(1).Width = 5.0;   // Nº
                ws.Column(2).Width = 14.0;  // Cédula
                ws.Column(3).Width = 36.0;  // Apellidos y Nombres
                ws.Column(4).Width = 7.5;   // Eval 1
                ws.Column(5).Width = 7.5;   // Eval 2
                ws.Column(6).Width = 7.5;   // Eval 3
                ws.Column(7).Width = 7.5;   // Eval 4
                ws.Column(8).Width = 7.5;   // Eval 5
                ws.Column(9).Width = 7.5;   // Eval 6
                ws.Column(10).Width = 8.0;  // RASGO
                ws.Column(11).Width = 12.0; // Definitiva del Lapso (100%)
                ws.Column(12).Width = 9.0;  // Ajuste Consejo
                ws.Column(13).Width = 9.0;  // Definitiva Final
                ws.Column(14).Width = 24.0; // Firma del Estudiante

                // ================= 1. ENCABEZADO INSTITUCIONAL (Igual a la imagen) =================
                ws.Range("A1:D1").Merge().Value = conf.Eponimo;
                ws.Range("A2:D2").Merge().Value = "Departamento de Control de Estudios y Evaluación";
                ws.Range("A3:D3").Merge().Value = $"Código Plantel: {conf.CodigoPlantel} - {conf.EntidadFederal}.";

                ws.Range("A1:D3").Style.Font.Bold = true;
                ws.Cell("A1").Style.Font.FontSize = 11;
                ws.Cell("A2").Style.Font.FontSize = 9.5;
                ws.Cell("A3").Style.Font.FontSize = 9;

                // Lado derecho de la cabecera
                ws.Range("J1:N1").Merge().Value = $"{gradoNombre.ToUpper()} \"{seccionNombre.ToUpper()}\"    {lapsoNombre.ToUpper()}";
                ws.Range("J2:N2").Merge().Value = $"Evaluación Continua - {lapsoNombre.ToUpper()}";
                ws.Range("J3:N3").Merge().Value = $"Año Escolar {periodoNombre}";

                for (int i = 1; i <= 3; i++)
                {
                    ws.Range(i, 10, i, 14).Style.Font.Bold = true;
                    ws.Range(i, 10, i, 14).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                }

                // Datos del Docente y Materia
                ws.Cell("A5").Value = "PROFESOR:";
                ws.Cell("A5").Style.Font.Bold = true;
                ws.Range("B5:E5").Merge().Value = docenteNombre.ToUpper();
                ws.Range("B5:E5").Style.Font.Bold = true;

                ws.Cell("A6").Value = "ASIGNATURA:";
                ws.Cell("A6").Style.Font.Bold = true;
                ws.Range("B6:E6").Merge().Value = materiaNombre.ToUpper();
                ws.Range("B6:E6").Style.Font.Bold = true;

                // ================= 2. ENCABEZADOS DE LA TABLA =================
                int filaH = 8;
                ws.Range("A8:A9").Merge().Value = "Nº";
                ws.Range("B8:B9").Merge().Value = "Cédula";
                ws.Range("C8:C9").Merge().Value = "Apellidos y Nombres";

                // 6 Columnas de Evaluaciones (Nombres de actividades)
                for (int i = 0; i < 6; i++)
                {
                    string actNom = (i < nombresEvaluaciones.Length && !string.IsNullOrWhiteSpace(nombresEvaluaciones[i]))
                        ? nombresEvaluaciones[i]
                        : $"Actividad {i + 1}";

                    int col = 4 + i;
                    var c = ws.Range(filaH, col, filaH + 1, col);
                    c.Merge().Value = actNom;
                    c.Style.Alignment.WrapText = true;
                }

                ws.Range("J8:J9").Merge().Value = "RASGO";
                ws.Range("K8:K9").Merge().Value = "Definitiva del\nLapso (100%)";
                ws.Range("L8:L9").Merge().Value = "AJUSTE DEL\nCONSEJO";
                ws.Range("M8:M9").Merge().Value = "DEFINITIVA\nDEL LAPSO";
                ws.Range("N8:N9").Merge().Value = "FIRMA DEL ESTUDIANTE";

                var rangoEncabezados = ws.Range(8, 1, 9, 14);
                rangoEncabezados.Style.Font.Bold = true;
                rangoEncabezados.Style.Font.FontSize = 8.5;
                rangoEncabezados.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
                rangoEncabezados.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                rangoEncabezados.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                rangoEncabezados.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rangoEncabezados.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                // ================= 3. FILAS DE ESTUDIANTES PRECARGADOS =================
                int r = 10;
                foreach (var est in estudiantes)
                {
                    ws.Cell(r, 1).Value = est.NroLista;
                    ws.Cell(r, 2).Value = est.Cedula;
                    ws.Cell(r, 3).Value = est.ApellidosYNombres;

                    // Notas si ya fueron ingresadas en el sistema
                    if (est.Eval1.HasValue) ws.Cell(r, 4).Value = est.Eval1.Value;
                    if (est.Eval2.HasValue) ws.Cell(r, 5).Value = est.Eval2.Value;
                    if (est.Eval3.HasValue) ws.Cell(r, 6).Value = est.Eval3.Value;
                    if (est.Eval4.HasValue) ws.Cell(r, 7).Value = est.Eval4.Value;
                    if (est.Eval5.HasValue) ws.Cell(r, 8).Value = est.Eval5.Value;
                    if (est.Eval6.HasValue) ws.Cell(r, 9).Value = est.Eval6.Value;

                    ws.Cell(r, 10).Value = ""; // Rasgo
                    ws.Cell(r, 11).Value = est.Definitiva > 0 ? est.Definitiva.ToString("D2") : "";
                    ws.Cell(r, 11).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF08A"); // Amarillo

                    ws.Cell(r, 12).Value = ""; // Ajuste consejo
                    ws.Cell(r, 13).Value = est.Definitiva > 0 ? est.Definitiva.ToString("D2") : ""; // Definitiva
                    ws.Cell(r, 14).Value = ""; // Espacio para la firma manual

                    ws.Cell(r, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(r, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    ws.Cell(r, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                    ws.Cell(r, 3).Style.Font.Bold = true;

                    for (int col = 4; col <= 13; col++)
                    {
                        ws.Cell(r, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }

                    ws.Row(r).Height = 20;
                    r++;
                }

                var rangoAlumnos = ws.Range(10, 1, r - 1, 14);
                rangoAlumnos.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                rangoAlumnos.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                rangoAlumnos.Style.Font.FontSize = 9;

                // ================= 4. CUADRO DE RESUMEN ESTADÍSTICO DEL LAPSO (Inferior) =================
                int rRes = r + 1;
                ws.Range(rRes, 4, rRes, 9).Merge().Value = "RESUMEN DEL LAPSO";
                ws.Range(rRes, 4, rRes, 9).Style.Font.Bold = true;
                ws.Range(rRes, 4, rRes, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Range(rRes, 4, rRes, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
                rRes++;

                string[] etiquetasResumen = {
            "N° APROBADOS EN CADA EVALUACIÓN",
            "N° APLAZADOS EN CADA EVALUACIÓN",
            "ASISTENTES",
            "INASISTENTES",
            "% DE APLAZADOS"
        };

                for (int i = 0; i < etiquetasResumen.Length; i++)
                {
                    ws.Range(rRes, 2, rRes, 3).Merge().Value = etiquetasResumen[i];
                    ws.Range(rRes, 2, rRes, 3).Style.Font.Bold = true;
                    ws.Range(rRes, 2, rRes, 3).Style.Font.FontSize = 8;
                    ws.Range(rRes, 2, rRes, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                    var rCuadros = ws.Range(rRes, 4, rRes, 9);
                    rCuadros.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    rCuadros.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    rRes++;
                }

                // ================= 5. FIRMAS =================
                rRes += 2;
                ws.Range(rRes, 2, rRes, 4).Merge().Value = "____________________________________";
                ws.Range(rRes, 10, rRes, 13).Merge().Value = "____________________________________";
                ws.Range(rRes + 1, 2, rRes + 1, 4).Merge().Value = $"PROFESOR: {docenteNombre.ToUpper()}";
                ws.Range(rRes + 1, 10, rRes + 1, 13).Merge().Value = "CONTROL DE ESTUDIOS Y EVALUACIÓN\nSELLO DEL PLANTEL";

                ws.Range(rRes, 1, rRes + 2, 14).Style.Font.Bold = true;
                ws.Range(rRes, 1, rRes + 2, 14).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Range(rRes + 1, 1, rRes + 2, 14).Style.Font.FontSize = 8.5;

                wb.SaveAs(rutaArchivo);
            }
        }
    }
}