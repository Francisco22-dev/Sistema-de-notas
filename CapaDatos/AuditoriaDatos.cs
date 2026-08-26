using System;
using MySqlConnector;

namespace SistemaLiceo.Datos
{
    public static class AuditoriaDatos
    {
        public static void Registrar(int usuarioId, string modulo, string accion)
        {
            try
            {
                string equipo = Environment.MachineName; // Identifica qué PC de la red hizo la acción
                ConexionBD con = new ConexionBD();

                using (MySqlConnection conexion = con.AbrirConexion())
                {
                    const string consulta = @"
                        INSERT INTO auditoria (usuario_id, modulo, accion, equipo) 
                        VALUES (@u, @m, @a, @e);";

                    using (MySqlCommand cmd = new MySqlCommand(consulta, conexion))
                    {
                        cmd.Parameters.AddWithValue("@u", usuarioId);
                        cmd.Parameters.AddWithValue("@m", modulo);
                        cmd.Parameters.AddWithValue("@a", accion);
                        cmd.Parameters.AddWithValue("@e", equipo);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                // La auditoría nunca debe frenar el flujo principal si ocurre un error menor
            }
        }
    }
}