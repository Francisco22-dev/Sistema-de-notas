using System;
using Entidades;
using MySqlConnector;

namespace SistemaLiceo.Datos
{
    /// <summary>Acceso a las tablas PERSONA y DIRECCION.</summary>
    public class PersonaDatos
    {
        private readonly ConexionBD _conexion = new ConexionBD();

        /// <summary>Inserta una direccion dentro de una transaccion en curso y devuelve su id.</summary>
        public static int InsertarDireccion(Direccion direccion, MySqlConnection conexion, MySqlTransaction transaccion)
        {
            const string consulta = @"
                INSERT INTO DIRECCION (ciudad_id, sector, avenida, calle, manzana, vereda,
                                       numero_vivienda, tipo_vivienda, condicion_vivienda, infraestructura_vivienda)
                VALUES (@ciudad, @sector, @avenida, @calle, @manzana, @vereda,
                        @numero, @tipo, @condicion, @infraestructura);
                SELECT LAST_INSERT_ID();";

            using (MySqlCommand comando = new MySqlCommand(consulta, conexion, transaccion))
            {
                comando.Parameters.AddWithValue("@ciudad", direccion.CiudadId);
                comando.Parameters.AddWithValue("@sector", Nulo(direccion.Sector));
                comando.Parameters.AddWithValue("@avenida", Nulo(direccion.Avenida));
                comando.Parameters.AddWithValue("@calle", Nulo(direccion.Calle));
                comando.Parameters.AddWithValue("@manzana", Nulo(direccion.Manzana));
                comando.Parameters.AddWithValue("@vereda", Nulo(direccion.Vereda));
                comando.Parameters.AddWithValue("@numero", Nulo(direccion.NumeroVivienda));
                comando.Parameters.AddWithValue("@tipo", direccion.TipoVivienda);
                comando.Parameters.AddWithValue("@condicion", direccion.CondicionVivienda);
                comando.Parameters.AddWithValue("@infraestructura", direccion.InfraestructuraVivienda);

                direccion.Id = Convert.ToInt32(comando.ExecuteScalar());
                return direccion.Id;
            }
        }

        /// <summary>Inserta una persona (y su direccion, si trae una) dentro de una transaccion en curso.</summary>
        public static int InsertarPersona(Persona persona, MySqlConnection conexion, MySqlTransaction transaccion)
        {
            if (persona.Direccion != null && persona.DireccionId == null)
                persona.DireccionId = InsertarDireccion(persona.Direccion, conexion, transaccion);

            const string consulta = @"
                INSERT INTO PERSONA (nacionalidad, cedula_identidad, nombre_1, nombre_2,
                                     apellido_1, apellido_2, fecha_nacimiento, sexo, direccion_id)
                VALUES (@nacionalidad, @cedula, @nombre1, @nombre2,
                        @apellido1, @apellido2, @fechaNacimiento, @sexo, @direccion);
                SELECT LAST_INSERT_ID();";

            using (MySqlCommand comando = new MySqlCommand(consulta, conexion, transaccion))
            {
                comando.Parameters.AddWithValue("@nacionalidad", persona.Nacionalidad);
                comando.Parameters.AddWithValue("@cedula", Nulo(persona.CedulaIdentidad));
                comando.Parameters.AddWithValue("@nombre1", persona.Nombre1);
                comando.Parameters.AddWithValue("@nombre2", Nulo(persona.Nombre2));
                comando.Parameters.AddWithValue("@apellido1", persona.Apellido1);
                comando.Parameters.AddWithValue("@apellido2", Nulo(persona.Apellido2));
                comando.Parameters.AddWithValue("@fechaNacimiento", (object?)persona.FechaNacimiento ?? DBNull.Value);
                comando.Parameters.AddWithValue("@sexo", persona.Sexo);
                comando.Parameters.AddWithValue("@direccion", (object?)persona.DireccionId ?? DBNull.Value);

                persona.Id = Convert.ToInt32(comando.ExecuteScalar());
                return persona.Id;
            }
        }

        /// <summary>Busca una persona por su cedula de identidad (sin prefijo de nacionalidad).</summary>
        public Persona? BuscarPorCedula(string cedula)
        {
            const string consulta = @"
                SELECT id, nacionalidad, cedula_identidad, nombre_1, nombre_2,
                       apellido_1, apellido_2, fecha_nacimiento, sexo, direccion_id
                FROM PERSONA
                WHERE cedula_identidad = @cedula
                LIMIT 1;";

            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@cedula", cedula);
                using (MySqlDataReader lector = comando.ExecuteReader())
                {
                    return lector.Read() ? Mapear(lector) : null;
                }
            }
        }
        /// <summary>Actualiza los datos personales de una persona.</summary>
        public static void ActualizarPersona(Persona persona, MySqlConnection conexion, MySqlTransaction transaccion)
        {
            if (persona.Direccion != null)
            {
                if (persona.DireccionId.HasValue && persona.DireccionId.Value > 0)
                {
                    persona.Direccion.Id = persona.DireccionId.Value;
                    ActualizarDireccion(persona.Direccion, conexion, transaccion);
                }
                else
                {
                    persona.DireccionId = InsertarDireccion(persona.Direccion, conexion, transaccion);
                }
            }

            const string consulta = @"
        UPDATE PERSONA 
        SET nacionalidad = @nacionalidad, 
            cedula_identidad = @cedula, 
            nombre_1 = @nombre1, 
            nombre_2 = @nombre2,
            apellido_1 = @apellido1, 
            apellido_2 = @apellido2, 
            fecha_nacimiento = @fechaNacimiento, 
            sexo = @sexo, 
            direccion_id = @direccion
        WHERE id = @id;";

            using (MySqlCommand comando = new MySqlCommand(consulta, conexion, transaccion))
            {
                comando.Parameters.AddWithValue("@id", persona.Id);
                comando.Parameters.AddWithValue("@nacionalidad", persona.Nacionalidad);
                comando.Parameters.AddWithValue("@cedula", Nulo(persona.CedulaIdentidad));
                comando.Parameters.AddWithValue("@nombre1", persona.Nombre1);
                comando.Parameters.AddWithValue("@nombre2", Nulo(persona.Nombre2));
                comando.Parameters.AddWithValue("@apellido1", persona.Apellido1);
                comando.Parameters.AddWithValue("@apellido2", Nulo(persona.Apellido2));
                comando.Parameters.AddWithValue("@fechaNacimiento", (object?)persona.FechaNacimiento ?? DBNull.Value);
                comando.Parameters.AddWithValue("@sexo", persona.Sexo);
                comando.Parameters.AddWithValue("@direccion", (object?)persona.DireccionId ?? DBNull.Value);

                comando.ExecuteNonQuery();
            }
        }

        /// <summary>Actualiza una dirección existente.</summary>
        public static void ActualizarDireccion(Direccion direccion, MySqlConnection conexion, MySqlTransaction transaccion)
        {
            const string consulta = @"
        UPDATE DIRECCION 
        SET ciudad_id = @ciudad, 
            sector = @sector, 
            avenida = @avenida, 
            calle = @calle, 
            manzana = @manzana, 
            vereda = @vereda,
            numero_vivienda = @numero, 
            tipo_vivienda = @tipo, 
            condicion_vivienda = @condicion, 
            infraestructura_vivienda = @infraestructura
        WHERE id = @id;";

            using (MySqlCommand comando = new MySqlCommand(consulta, conexion, transaccion))
            {
                comando.Parameters.AddWithValue("@id", direccion.Id);
                comando.Parameters.AddWithValue("@ciudad", direccion.CiudadId);
                comando.Parameters.AddWithValue("@sector", Nulo(direccion.Sector));
                comando.Parameters.AddWithValue("@avenida", Nulo(direccion.Avenida));
                comando.Parameters.AddWithValue("@calle", Nulo(direccion.Calle));
                comando.Parameters.AddWithValue("@manzana", Nulo(direccion.Manzana));
                comando.Parameters.AddWithValue("@vereda", Nulo(direccion.Vereda));
                comando.Parameters.AddWithValue("@numero", Nulo(direccion.NumeroVivienda));
                comando.Parameters.AddWithValue("@tipo", direccion.TipoVivienda);
                comando.Parameters.AddWithValue("@condicion", direccion.CondicionVivienda);
                comando.Parameters.AddWithValue("@infraestructura", direccion.InfraestructuraVivienda);

                comando.ExecuteNonQuery();
            }
        }

        internal static Persona Mapear(MySqlDataReader lector, string prefijo = "")
        {
            return new Persona
            {
                Id = lector.GetInt32(prefijo + "id"),
                Nacionalidad = lector.GetString(prefijo + "nacionalidad"),
                CedulaIdentidad = lector.IsDBNull(lector.GetOrdinal(prefijo + "cedula_identidad"))
                    ? null : lector.GetString(prefijo + "cedula_identidad"),
                Nombre1 = lector.GetString(prefijo + "nombre_1"),
                Nombre2 = lector.IsDBNull(lector.GetOrdinal(prefijo + "nombre_2")) ? null : lector.GetString(prefijo + "nombre_2"),
                Apellido1 = lector.GetString(prefijo + "apellido_1"),
                Apellido2 = lector.IsDBNull(lector.GetOrdinal(prefijo + "apellido_2")) ? null : lector.GetString(prefijo + "apellido_2"),
                FechaNacimiento = lector.IsDBNull(lector.GetOrdinal(prefijo + "fecha_nacimiento"))
                    ? null : lector.GetDateTime(prefijo + "fecha_nacimiento"),
                Sexo = lector.GetString(prefijo + "sexo"),
                DireccionId = lector.IsDBNull(lector.GetOrdinal(prefijo + "direccion_id")) ? null : lector.GetInt32(prefijo + "direccion_id")
            };
        }

        internal static object Nulo(string? texto) =>
            string.IsNullOrWhiteSpace(texto) ? DBNull.Value : texto.Trim();
    }
}
