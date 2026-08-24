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
    /// <summary>Acceso a datos para PROFESOR, MATERIA y asignaciones académicas.</summary>
    public class ProfesorDatos
    {
        private readonly ConexionBD _conexion = new ConexionBD();

        public int Registrar(Profesor profesor)
        {
            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlTransaction transaccion = conexion.BeginTransaction())
            {
                try
                {
                    // 1. Verificar si la persona ya existe en la base de datos (Ej. es Representante)
                    Persona? personaExistente = PersonaDatos.BuscarPorCedula(profesor.Persona.CedulaIdentidad ?? string.Empty, conexion, transaccion);
                    if (personaExistente != null)
                    {
                        profesor.PersonaId = personaExistente.Id;
                        profesor.Persona.Id = personaExistente.Id;
                        profesor.Persona.DireccionId = personaExistente.DireccionId;
                        PersonaDatos.ActualizarPersona(profesor.Persona, conexion, transaccion);
                    }
                    else
                    {
                        profesor.PersonaId = PersonaDatos.InsertarPersona(profesor.Persona, conexion, transaccion);
                    }

                    // 2. Verificar que no esté ya duplicado en la tabla PROFESOR
                    const string busquedaProf = "SELECT id FROM PROFESOR WHERE persona_id = @p LIMIT 1;";
                    using (MySqlCommand cmdCheck = new MySqlCommand(busquedaProf, conexion, transaccion))
                    {
                        cmdCheck.Parameters.AddWithValue("@p", profesor.PersonaId);
                        object? res = cmdCheck.ExecuteScalar();
                        if (res != null && res != DBNull.Value)
                            throw new Exception("Esta persona ya se encuentra registrada en la plantilla de profesores.");
                    }

                    // 3. Insertar el registro en PROFESOR
                    const string consulta = @"
                INSERT INTO PROFESOR (tipo_nivel, persona_id, ESTADO)
                VALUES (@tipoNivel, @personaId, @estado);
                SELECT LAST_INSERT_ID();";

                    using (MySqlCommand comando = new MySqlCommand(consulta, conexion, transaccion))
                    {
                        comando.Parameters.AddWithValue("@tipoNivel", profesor.TipoNivel);
                        comando.Parameters.AddWithValue("@personaId", profesor.PersonaId);
                        comando.Parameters.AddWithValue("@estado", profesor.Estado);

                        profesor.Id = Convert.ToInt32(comando.ExecuteScalar());
                    }

                    transaccion.Commit();
                    return profesor.Id;
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

        public void Actualizar(Profesor profesor)
        {
            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlTransaction transaccion = conexion.BeginTransaction())
            {
                try
                {
                    PersonaDatos.ActualizarPersona(profesor.Persona, conexion, transaccion);

                    const string consulta = @"
                        UPDATE PROFESOR
                        SET tipo_nivel = @tipoNivel, ESTADO = @estado
                        WHERE id = @id;";

                    using (MySqlCommand comando = new MySqlCommand(consulta, conexion, transaccion))
                    {
                        comando.Parameters.AddWithValue("@id", profesor.Id);
                        comando.Parameters.AddWithValue("@tipoNivel", profesor.TipoNivel);
                        comando.Parameters.AddWithValue("@estado", profesor.Estado);
                        comando.ExecuteNonQuery();
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

        public Profesor? BuscarPorId(int profesorId)
        {
            const string consulta = @"
        SELECT pr.id, pr.tipo_nivel, pr.persona_id, pr.ESTADO,
               p.id AS p_id, 
               p.nacionalidad, 
               p.cedula_identidad, 
               p.nombre_1, 
               p.nombre_2,
               p.apellido_1, 
               p.apellido_2, 
               p.fecha_nacimiento, 
               p.sexo, 
               p.direccion_id
        FROM PROFESOR pr
        INNER JOIN PERSONA p ON p.id = pr.persona_id
        WHERE pr.id = @id LIMIT 1;";

            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@id", profesorId);
                using (MySqlDataReader lector = comando.ExecuteReader())
                {
                    if (!lector.Read()) return null;

                    return new Profesor
                    {
                        Id = lector.GetInt32("id"),
                        TipoNivel = lector.GetString("tipo_nivel"),
                        PersonaId = lector.GetInt32("persona_id"),
                        Estado = lector.GetString("ESTADO"),
                        Persona = new Persona
                        {
                            Id = lector.GetInt32("p_id"),
                            Nacionalidad = lector.GetString("nacionalidad"),
                            CedulaIdentidad = lector.IsDBNull(lector.GetOrdinal("cedula_identidad")) ? null : lector.GetString("cedula_identidad"),
                            Nombre1 = lector.GetString("nombre_1"),
                            Nombre2 = lector.IsDBNull(lector.GetOrdinal("nombre_2")) ? null : lector.GetString("nombre_2"),
                            Apellido1 = lector.GetString("apellido_1"),
                            Apellido2 = lector.IsDBNull(lector.GetOrdinal("apellido_2")) ? null : lector.GetString("apellido_2"),
                            FechaNacimiento = lector.IsDBNull(lector.GetOrdinal("fecha_nacimiento")) ? null : lector.GetDateTime("fecha_nacimiento"),
                            Sexo = lector.GetString("sexo"),
                            DireccionId = lector.IsDBNull(lector.GetOrdinal("direccion_id")) ? null : lector.GetInt32("direccion_id")
                        }
                    };
                }
            }
        }

        public DataTable ListarActivos()
        {
            const string consulta = @"
                SELECT pr.id AS Codigo,
                       CONCAT(p.nacionalidad, '-', IFNULL(p.cedula_identidad, '')) AS Cedula,
                       CONCAT_WS(' ', p.nombre_1, p.nombre_2, p.apellido_1, p.apellido_2) AS Profesor,
                       pr.tipo_nivel AS Nivel,
                       pr.ESTADO AS Estado
                FROM PROFESOR pr
                INNER JOIN PERSONA p ON p.id = pr.persona_id
                WHERE pr.ESTADO = 'Activo'
                ORDER BY p.apellido_1, p.nombre_1;";

            DataTable tabla = new DataTable();
            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlDataAdapter adaptador = new MySqlDataAdapter(consulta, conexion))
            {
                adaptador.Fill(tabla);
            }
            return tabla;
        }

        public int AsignarMateriaAProfesor(int profesorId, int materiaId)
        {
            using (MySqlConnection conexion = _conexion.AbrirConexion())
            {
                const string busqueda = "SELECT id FROM MATERIA_PROFESOR WHERE profesor_id = @p AND materia_id = @m LIMIT 1;";
                using (MySqlCommand comando = new MySqlCommand(busqueda, conexion))
                {
                    comando.Parameters.AddWithValue("@p", profesorId);
                    comando.Parameters.AddWithValue("@m", materiaId);
                    object? resultado = comando.ExecuteScalar();
                    if (resultado != null && resultado != DBNull.Value)
                        return Convert.ToInt32(resultado);
                }

                const string insercion = @"INSERT INTO MATERIA_PROFESOR (profesor_id, materia_id) VALUES (@p, @m);
                                           SELECT LAST_INSERT_ID();";
                using (MySqlCommand comando = new MySqlCommand(insercion, conexion))
                {
                    comando.Parameters.AddWithValue("@p", profesorId);
                    comando.Parameters.AddWithValue("@m", materiaId);
                    return Convert.ToInt32(comando.ExecuteScalar());
                }
            }
        }

        public int AsignarMateriaSeccionPeriodo(int gradoSeccionId, int gradoMateriaId, int materiaProfesorId, int periodoId)
        {
            using (MySqlConnection conexion = _conexion.AbrirConexion())
            {
                const string busqueda = @"
                    SELECT id FROM MATERIA_PROFESOR_PERIODO 
                    WHERE grado_seccion_id = @gs AND grado_materia_id = @gm AND periodo_id = @per LIMIT 1;";

                using (MySqlCommand comando = new MySqlCommand(busqueda, conexion))
                {
                    comando.Parameters.AddWithValue("@gs", gradoSeccionId);
                    comando.Parameters.AddWithValue("@gm", gradoMateriaId);
                    comando.Parameters.AddWithValue("@per", periodoId);
                    object? resultado = comando.ExecuteScalar();
                    if (resultado != null && resultado != DBNull.Value)
                        return Convert.ToInt32(resultado);
                }

                const string insercion = @"
                    INSERT INTO MATERIA_PROFESOR_PERIODO (grado_seccion_id, grado_materia_id, materia_profesor_id, periodo_id)
                    VALUES (@gs, @gm, @mp, @per);
                    SELECT LAST_INSERT_ID();";

                using (MySqlCommand comando = new MySqlCommand(insercion, conexion))
                {
                    comando.Parameters.AddWithValue("@gs", gradoSeccionId);
                    comando.Parameters.AddWithValue("@gm", gradoMateriaId);
                    comando.Parameters.AddWithValue("@mp", materiaProfesorId);
                    comando.Parameters.AddWithValue("@per", periodoId);
                    return Convert.ToInt32(comando.ExecuteScalar());
                }
            }
        }

        public List<MateriaProfesorPeriodo> ListarCargasAcademicas(int periodoId, int gradoSeccionId = 0)
        {
            List<MateriaProfesorPeriodo> lista = new List<MateriaProfesorPeriodo>();

            const string consulta = @"
                SELECT mpp.id, mpp.grado_seccion_id, mpp.grado_materia_id, mpp.materia_profesor_id, mpp.periodo_id,
                       g.nombre AS grado, s.nombre AS seccion, m.nombre AS materia,
                       CONCAT_WS(' ', p.nombre_1, p.apellido_1) AS docente,
                       pa.nombre AS periodo
                FROM MATERIA_PROFESOR_PERIODO mpp
                INNER JOIN GRADO_SECCION gs ON gs.id = mpp.grado_seccion_id
                INNER JOIN GRADO g ON g.id = gs.grado_id
                INNER JOIN SECCION s ON s.id = gs.seccion_id
                INNER JOIN GRADO_MATERIA gm ON gm.id = mpp.grado_materia_id
                INNER JOIN MATERIA m ON m.id = gm.materia_id
                INNER JOIN MATERIA_PROFESOR mp ON mp.id = mpp.materia_profesor_id
                INNER JOIN PROFESOR pr ON pr.id = mp.profesor_id
                INNER JOIN PERSONA p ON p.id = pr.persona_id
                INNER JOIN PERIODO_ACADEMICO pa ON pa.id = mpp.periodo_id
                WHERE mpp.periodo_id = @periodo AND (@gradoSeccion = 0 OR mpp.grado_seccion_id = @gradoSeccion)
                ORDER BY g.id, s.nombre, m.nombre;";

            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@periodo", periodoId);
                comando.Parameters.AddWithValue("@gradoSeccion", gradoSeccionId);

                using (MySqlDataReader lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        lista.Add(new MateriaProfesorPeriodo
                        {
                            Id = lector.GetInt32("id"),
                            GradoSeccionId = lector.GetInt32("grado_seccion_id"),
                            GradoMateriaId = lector.GetInt32("grado_materia_id"),
                            MateriaProfesorId = lector.GetInt32("materia_profesor_id"),
                            PeriodoId = lector.GetInt32("periodo_id"),
                            Grado = lector.GetString("grado"),
                            Seccion = lector.GetString("seccion"),
                            Materia = lector.GetString("materia"),
                            Docente = lector.GetString("docente"),
                            Periodo = lector.GetString("periodo")
                        });
                    }
                }
            }

            return lista;
        }
    }
}
