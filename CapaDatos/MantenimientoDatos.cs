using System;
using Entidades;
using MySqlConnector;

namespace SistemaLiceo.Datos
{
    public class MantenimientoDatos
    {
        private readonly ConexionBD _conexion = new ConexionBD();

        public SistemaMantenimiento ObtenerConfiguracion()
        {
            const string consulta = @"
                SELECT id, bloqueo_manual, fecha_limite, frecuencia_meses, mensaje
                FROM sistema_mantenimiento
                WHERE id = 1 LIMIT 1;";

            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            using (MySqlDataReader lector = comando.ExecuteReader())
            {
                if (!lector.Read()) return new SistemaMantenimiento();

                return new SistemaMantenimiento
                {
                    Id = lector.GetInt32("id"),
                    BloqueoManual = lector.GetBoolean("bloqueo_manual"),
                    FechaLimite = lector.GetDateTime("fecha_limite"),
                    FrecuenciaMeses = lector.GetInt32("frecuencia_meses"),
                    Mensaje = lector.GetString("mensaje")
                };
            }
        }

        public void GuardarConfiguracion(SistemaMantenimiento config)
        {
            const string consulta = @"
                INSERT INTO sistema_mantenimiento (id, bloqueo_manual, fecha_limite, frecuencia_meses, mensaje)
                VALUES (1, @bloqueo, @fecha, @meses, @mensaje)
                ON DUPLICATE KEY UPDATE
                    bloqueo_manual = VALUES(bloqueo_manual),
                    fecha_limite = VALUES(fecha_limite),
                    frecuencia_meses = VALUES(frecuencia_meses),
                    mensaje = VALUES(mensaje);";

            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@bloqueo", config.BloqueoManual);
                comando.Parameters.AddWithValue("@fecha", config.FechaLimite);
                comando.Parameters.AddWithValue("@meses", config.FrecuenciaMeses);
                comando.Parameters.AddWithValue("@mensaje", config.Mensaje.Trim());
                comando.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Verifica si el sistema se encuentra bloqueado para usuarios regulares.
        /// </summary>
        public bool EstaBloqueadoElSistema(out string mensaje)
        {
            mensaje = string.Empty;
            try
            {
                SistemaMantenimiento conf = ObtenerConfiguracion();

                // Se bloquea si se activó el switch manual o si ya se cumplió la fecha y hora límite
                if (conf.BloqueoManual || DateTime.Now >= conf.FechaLimite)
                {
                    mensaje = conf.Mensaje;
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}