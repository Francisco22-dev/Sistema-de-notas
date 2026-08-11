using System;
using System.Collections.Generic;
using System.Data;
using Entidades;
using MySqlConnector;

namespace SistemaLiceo.Datos
{
    /// <summary>Acceso a la tabla PERSONA_REPRESENTANTE.</summary>
    public class RepresentanteDatos
    {
        private readonly ConexionBD _conexion = new ConexionBD();

        /// <summary>Registra la persona y su ficha de representante. Devuelve el id de PERSONA_REPRESENTANTE.</summary>
        public int Registrar(Representante representante)
        {
            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlTransaction transaccion = conexion.BeginTransaction())
            {
                try
                {
                    int id = Insertar(representante, conexion, transaccion);
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

        /// <summary>Inserta el representante dentro de una transaccion en curso.</summary>
        public static int Insertar(Representante representante, MySqlConnection conexion, MySqlTransaction transaccion)
        {
            if (representante.PersonaId == 0)
                representante.PersonaId = PersonaDatos.InsertarPersona(representante.Persona, conexion, transaccion);

            const string consulta = @"
                INSERT INTO PERSONA_REPRESENTANTE (parentesco, estado_civil, ingreso_mensual, telefono_movil,
                                                   telefono_habitacion, correo_electronico, profesion,
                                                   empresa_trabajo, telefono_empresa, direccion_empresa,
                                                   persona_id, ESTADO)
                VALUES (@parentesco, @estadoCivil, @ingreso, @movil, @habitacion, @correo, @profesion,
                        @empresa, @telefonoEmpresa, @direccionEmpresa, @persona, @estado);
                SELECT LAST_INSERT_ID();";

            using (MySqlCommand comando = new MySqlCommand(consulta, conexion, transaccion))
            {
                comando.Parameters.AddWithValue("@parentesco", representante.Parentesco);
                comando.Parameters.AddWithValue("@estadoCivil", representante.EstadoCivil);
                comando.Parameters.AddWithValue("@ingreso", (object?)representante.IngresoMensual ?? DBNull.Value);
                comando.Parameters.AddWithValue("@movil", PersonaDatos.Nulo(representante.TelefonoMovil));
                comando.Parameters.AddWithValue("@habitacion", PersonaDatos.Nulo(representante.TelefonoHabitacion));
                comando.Parameters.AddWithValue("@correo", PersonaDatos.Nulo(representante.CorreoElectronico));
                comando.Parameters.AddWithValue("@profesion", PersonaDatos.Nulo(representante.Profesion));
                comando.Parameters.AddWithValue("@empresa", PersonaDatos.Nulo(representante.EmpresaTrabajo));
                comando.Parameters.AddWithValue("@telefonoEmpresa", PersonaDatos.Nulo(representante.TelefonoEmpresa));
                comando.Parameters.AddWithValue("@direccionEmpresa", PersonaDatos.Nulo(representante.DireccionEmpresa));
                comando.Parameters.AddWithValue("@persona", representante.PersonaId);
                comando.Parameters.AddWithValue("@estado", representante.Estado);

                representante.Id = Convert.ToInt32(comando.ExecuteScalar());
                return representante.Id;
            }
        }

        /// <summary>Busca un representante activo por la cedula de la persona.</summary>
        public Representante? BuscarPorCedula(string cedula)
        {
            const string consulta = @"
                SELECT r.id, r.parentesco, r.estado_civil, r.ingreso_mensual, r.telefono_movil,
                       r.telefono_habitacion, r.correo_electronico, r.profesion, r.empresa_trabajo,
                       r.telefono_empresa, r.direccion_empresa, r.persona_id, r.ESTADO,
                       p.id AS p_id, p.nacionalidad, p.cedula_identidad, p.nombre_1, p.nombre_2,
                       p.apellido_1, p.apellido_2, p.fecha_nacimiento, p.sexo, p.direccion_id
                FROM PERSONA_REPRESENTANTE r
                INNER JOIN PERSONA p ON p.id = r.persona_id
                WHERE p.cedula_identidad = @cedula AND r.ESTADO = 'Activo'
                LIMIT 1;";

            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@cedula", cedula);
                using (MySqlDataReader lector = comando.ExecuteReader())
                {
                    if (!lector.Read())
                        return null;

                    Representante representante = new Representante
                    {
                        Id = lector.GetInt32("id"),
                        Parentesco = lector.GetString("parentesco"),
                        EstadoCivil = lector.GetString("estado_civil"),
                        IngresoMensual = lector.IsDBNull(lector.GetOrdinal("ingreso_mensual")) ? null : lector.GetDecimal("ingreso_mensual"),
                        TelefonoMovil = Texto(lector, "telefono_movil"),
                        TelefonoHabitacion = Texto(lector, "telefono_habitacion"),
                        CorreoElectronico = Texto(lector, "correo_electronico"),
                        Profesion = Texto(lector, "profesion"),
                        EmpresaTrabajo = Texto(lector, "empresa_trabajo"),
                        TelefonoEmpresa = Texto(lector, "telefono_empresa"),
                        DireccionEmpresa = Texto(lector, "direccion_empresa"),
                        PersonaId = lector.GetInt32("persona_id"),
                        Estado = lector.GetString("ESTADO")
                    };

                    representante.Persona = new Persona
                    {
                        Id = lector.GetInt32("p_id"),
                        Nacionalidad = lector.GetString("nacionalidad"),
                        CedulaIdentidad = Texto(lector, "cedula_identidad"),
                        Nombre1 = lector.GetString("nombre_1"),
                        Nombre2 = Texto(lector, "nombre_2"),
                        Apellido1 = lector.GetString("apellido_1"),
                        Apellido2 = Texto(lector, "apellido_2"),
                        FechaNacimiento = lector.IsDBNull(lector.GetOrdinal("fecha_nacimiento")) ? null : lector.GetDateTime("fecha_nacimiento"),
                        Sexo = lector.GetString("sexo"),
                        DireccionId = lector.IsDBNull(lector.GetOrdinal("direccion_id")) ? null : lector.GetInt32("direccion_id")
                    };

                    return representante;
                }
            }
        }

        /// <summary>Listado para grillas: representantes activos con sus datos personales.</summary>
        public DataTable ListarActivos()
        {
            const string consulta = @"
                SELECT r.id AS Codigo,
                       CONCAT(p.nacionalidad, '-', IFNULL(p.cedula_identidad, '')) AS Cedula,
                       CONCAT_WS(' ', p.nombre_1, p.nombre_2, p.apellido_1, p.apellido_2) AS Representante,
                       r.parentesco AS Parentesco,
                       r.telefono_movil AS Telefono,
                       r.correo_electronico AS Correo
                FROM PERSONA_REPRESENTANTE r
                INNER JOIN PERSONA p ON p.id = r.persona_id
                WHERE r.ESTADO = 'Activo'
                ORDER BY p.apellido_1, p.nombre_1;";

            DataTable tabla = new DataTable();
            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlDataAdapter adaptador = new MySqlDataAdapter(consulta, conexion))
            {
                adaptador.Fill(tabla);
            }
            return tabla;
        }

        private static string? Texto(MySqlDataReader lector, string columna) =>
            lector.IsDBNull(lector.GetOrdinal(columna)) ? null : lector.GetString(columna);
    }
}
