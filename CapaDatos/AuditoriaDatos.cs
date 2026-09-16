using System;
using System.Data;
using MySqlConnector;

namespace SistemaLiceo.Datos
{
    public static class AuditoriaDatos
    {
        private static readonly ConexionBD _conexion = new ConexionBD();

        public static void Registrar(int usuarioId, string modulo, string accion)
        {
            try
            {
                // Captura el nombre de la PC física en la red
                string equipo = Environment.MachineName;
                if (string.IsNullOrWhiteSpace(equipo))
                    equipo = Environment.GetEnvironmentVariable("COMPUTERNAME") ?? "PC-LOCAL";

                // Si por alguna razón no hay sesión activa, asociar al ID 1 (Admin)
                int idValido = usuarioId > 0 ? usuarioId : 1;

                using (MySqlConnection conexion = _conexion.AbrirConexion())
                {
                    const string consulta = @"
                        INSERT INTO auditoria (usuario_id, modulo, accion, equipo) 
                        VALUES (@u, @m, @a, @e);";

                    using (MySqlCommand cmd = new MySqlCommand(consulta, conexion))
                    {
                        cmd.Parameters.AddWithValue("@u", idValido);
                        cmd.Parameters.AddWithValue("@m", modulo);
                        cmd.Parameters.AddWithValue("@a", accion);
                        cmd.Parameters.AddWithValue("@e", equipo);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                // La auditoría no interrumpe el flujo si ocurre un error menor
            }
        }

        public static DataTable ListarLogs(string? busqueda = null)
        {
            string consulta = @"
                SELECT a.id AS 'ID',
                       u.nombre AS 'Usuario',
                       u.rol AS 'Rol',
                       a.modulo AS 'Módulo',
                       a.accion AS 'Acción Realizada',
                       IFNULL(a.equipo, 'PC-DESCONOCIDA') AS 'Equipo / PC',
                       a.fecha AS 'Fecha y Hora'
                FROM auditoria a
                INNER JOIN USUARIO u ON u.id = a.usuario_id
                WHERE (@b IS NULL OR u.nombre LIKE @b OR a.modulo LIKE @b OR a.accion LIKE @b OR a.equipo LIKE @b)
                ORDER BY a.fecha DESC
                LIMIT 500;";

            DataTable tabla = new DataTable();
            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand cmd = new MySqlCommand(consulta, conexion))
            {
                cmd.Parameters.AddWithValue("@b", string.IsNullOrWhiteSpace(busqueda) ? DBNull.Value : $"%{busqueda.Trim()}%");
                using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                {
                    da.Fill(tabla);
                }
            }
            return tabla;
        }

        public static void LimpiarLogs()
        {
            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand cmd = new MySqlCommand("TRUNCATE TABLE auditoria;", conexion))
            {
                cmd.ExecuteNonQuery();
            }
        }
    }
}