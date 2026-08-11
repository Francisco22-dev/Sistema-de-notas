using System;
using System.Data;
using Entidades;
using MySqlConnector;

namespace SistemaLiceo.Datos
{
    /// <summary>Acceso a la tabla USUARIO.</summary>
    public class UsuarioDatos
    {
        private readonly ConexionBD _conexion = new ConexionBD();

        /// <summary>
        /// Valida las credenciales contra USUARIO. La contrasena viaja ya cifrada
        /// (SHA-256) desde la capa de negocio y se compara con la columna pass.
        /// </summary>
        public Usuario? Autenticar(string nombre, string claveCifrada)
        {
            const string consulta = @"
                SELECT id, nombre, rol, ESTADO
                FROM USUARIO
                WHERE nombre = @nombre AND pass = @pass AND ESTADO = 'Activo'
                LIMIT 1;";

            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@nombre", nombre);
                comando.Parameters.AddWithValue("@pass", claveCifrada);

                using (MySqlDataReader lector = comando.ExecuteReader())
                {
                    if (!lector.Read())
                        return null;

                    return new Usuario
                    {
                        Id = lector.GetInt32("id"),
                        Nombre = lector.GetString("nombre"),
                        Rol = lector.GetString("rol"),
                        Estado = lector.GetString("ESTADO")
                    };
                }
            }
        }

        /// <summary>Crea un usuario del sistema. La clave debe llegar ya cifrada.</summary>
        public int Registrar(Usuario usuario, string claveCifrada)
        {
            const string consulta = @"
                INSERT INTO USUARIO (nombre, rol, pass, ESTADO)
                VALUES (@nombre, @rol, @pass, @estado);
                SELECT LAST_INSERT_ID();";

            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@nombre", usuario.Nombre);
                comando.Parameters.AddWithValue("@rol", usuario.Rol);
                comando.Parameters.AddWithValue("@pass", claveCifrada);
                comando.Parameters.AddWithValue("@estado", usuario.Estado);

                try
                {
                    usuario.Id = Convert.ToInt32(comando.ExecuteScalar());
                    return usuario.Id;
                }
                catch (MySqlException ex)
                {
                    throw new Exception(ConexionBD.TraducirError(ex), ex);
                }
            }
        }

        public DataTable ListarActivos()
        {
            const string consulta = @"SELECT id AS Codigo, nombre AS Usuario, rol AS Rol, ESTADO AS Estado
                                      FROM USUARIO WHERE ESTADO = 'Activo' ORDER BY nombre;";

            DataTable tabla = new DataTable();
            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlDataAdapter adaptador = new MySqlDataAdapter(consulta, conexion))
            {
                adaptador.Fill(tabla);
            }
            return tabla;
        }
    }
}
