using System;
using System.Data;
using Entidades;
using MySqlConnector;

namespace SistemaLiceo.Datos
{
    public class UsuarioDatos
    {
        private readonly ConexionBD _conexion = new ConexionBD();

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

        public Usuario? BuscarPorId(int id)
        {
            const string consulta = "SELECT id, nombre, rol, ESTADO FROM USUARIO WHERE id = @id LIMIT 1;";

            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@id", id);
                using (MySqlDataReader lector = comando.ExecuteReader())
                {
                    if (!lector.Read()) return null;

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

        public int Registrar(Usuario usuario, string claveCifrada)
        {
            const string consulta = @"
                INSERT INTO USUARIO (nombre, rol, pass, ESTADO)
                VALUES (@nombre, @rol, @pass, @estado);
                SELECT LAST_INSERT_ID();";

            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@nombre", usuario.Nombre.Trim());
                comando.Parameters.AddWithValue("@rol", usuario.Rol);
                comando.Parameters.AddWithValue("@pass", claveCifrada);
                comando.Parameters.AddWithValue("@estado", usuario.Estado);

                usuario.Id = Convert.ToInt32(comando.ExecuteScalar());
                return usuario.Id;
            }
        }

        public void Actualizar(Usuario usuario, string? nuevaClaveCifrada = null)
        {
            string consulta = string.IsNullOrWhiteSpace(nuevaClaveCifrada)
                ? "UPDATE USUARIO SET nombre = @nombre, rol = @rol, ESTADO = @estado WHERE id = @id;"
                : "UPDATE USUARIO SET nombre = @nombre, rol = @rol, pass = @pass, ESTADO = @estado WHERE id = @id;";

            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@id", usuario.Id);
                comando.Parameters.AddWithValue("@nombre", usuario.Nombre.Trim());
                comando.Parameters.AddWithValue("@rol", usuario.Rol);
                comando.Parameters.AddWithValue("@estado", usuario.Estado);
                if (!string.IsNullOrWhiteSpace(nuevaClaveCifrada))
                    comando.Parameters.AddWithValue("@pass", nuevaClaveCifrada);

                comando.ExecuteNonQuery();
            }
        }

        public void CambiarEstado(int id, string nuevoEstado)
        {
            const string consulta = "UPDATE USUARIO SET ESTADO = @estado WHERE id = @id;";
            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@id", id);
                comando.Parameters.AddWithValue("@estado", nuevoEstado);
                comando.ExecuteNonQuery();
            }
        }

        public bool ExisteNombreUsuario(string nombre, int idExcluir = 0)
        {
            const string consulta = "SELECT 1 FROM USUARIO WHERE nombre = @nombre AND id <> @id LIMIT 1;";
            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@nombre", nombre.Trim());
                comando.Parameters.AddWithValue("@id", idExcluir);
                return comando.ExecuteScalar() != null;
            }
        }

        public DataTable ListarTodos()
        {
            const string consulta = @"
                SELECT id AS Codigo, 
                       nombre AS Usuario, 
                       rol AS Rol, 
                       ESTADO AS Estado,
                       create_at AS 'Fecha de Registro'
                FROM USUARIO 
                ORDER BY ESTADO ASC, nombre ASC;";

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