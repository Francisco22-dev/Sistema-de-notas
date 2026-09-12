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

                List<decimal> notasParaPromedio = new List<decimal>();

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

                            if (nota.HasValue) notasParaPromedio.Add(nota.Value);

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
                dto.PromedioGeneral = notasParaPromedio.Count > 0 ? Math.Round(notasParaPromedio.Average(), 3) : 20.000m;
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
    }
}