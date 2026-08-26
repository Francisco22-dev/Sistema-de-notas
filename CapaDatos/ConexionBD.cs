using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using MySqlConnector;

namespace SistemaLiceo.Datos
{
    public class ConexionBD
    {
        private const string ArchivoConfiguracion = "conexion.config";
        private static readonly object Candado = new object();
        private static string? _cadenaConexion;

        public static string CadenaConexion
        {
            get
            {
                if (_cadenaConexion == null)
                {
                    lock (Candado)
                    {
                        _cadenaConexion ??= ConstruirCadena();
                    }
                }
                return _cadenaConexion;
            }
            set => _cadenaConexion = value;
        }

        private static string ConstruirCadena()
        {
            Dictionary<string, string> valores = LeerArchivoConfiguracion();

            MySqlConnectionStringBuilder constructor = new MySqlConnectionStringBuilder
            {
                Server = Obtener(valores, "Server", "127.0.0.1"),
                Port = Convert.ToUInt32(Obtener(valores, "Port", "3306")),
                Database = Obtener(valores, "Database", "db_carabobo"),
                UserID = Obtener(valores, "Uid", "usuario_secretaria"),
                Password = Obtener(valores, "Pwd", ""),

                // ⚡ PARÁMETROS CRÍTICOS PARA RED LOCAL (SWITCH)
                ConnectionTimeout = 6,          // No congela la UI si el switch se desconecta
                DefaultCommandTimeout = 30,     // Timeout para consultas pesadas
                Keepalive = 5,                  // Envía paquetes TCP cada 5 segs para no perder la conexión
                Pooling = true,
                MinimumPoolSize = 2,            // Mantiene 2 conexiones calientes por PC
                MaximumPoolSize = 30,           // Límite prudente por cliente
                ConnectionIdleTimeout = 180,    // Libera conexiones inactivas
                CharacterSet = "utf8mb4",
                AllowUserVariables = true,
                ConvertZeroDateTime = true
            };

            return constructor.ConnectionString;
        }

        private static Dictionary<string, string> LeerArchivoConfiguracion()
        {
            Dictionary<string, string> valores = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string ruta = Path.Combine(AppContext.BaseDirectory, ArchivoConfiguracion);

            if (!File.Exists(ruta))
                return valores;

            foreach (string linea in File.ReadAllLines(ruta))
            {
                string texto = linea.Trim();
                if (texto.Length == 0 || texto.StartsWith("#") || texto.StartsWith(";"))
                    continue;

                int separador = texto.IndexOf('=');
                if (separador <= 0)
                    continue;

                valores[texto.Substring(0, separador).Trim()] = texto.Substring(separador + 1).Trim();
            }

            return valores;
        }

        private static string Obtener(Dictionary<string, string> valores, string clave, string porDefecto)
        {
            return valores.TryGetValue(clave, out string? valor) && valor.Length > 0 ? valor : porDefecto;
        }

        public MySqlConnection ObtenerConexion() => new MySqlConnection(CadenaConexion);

        public MySqlConnection AbrirConexion()
        {
            MySqlConnection conexion = new MySqlConnection(CadenaConexion);
            conexion.Open();
            return conexion;
        }

        /// <summary>
        /// Ejecuta una acción transaccional con reintento automático en caso de micro-cortes o Deadlocks de red.
        /// </summary>
        public static T EjecutarConReintento<T>(Func<T> accion, int maxReintentos = 3)
        {
            int intento = 0;
            while (true)
            {
                try
                {
                    intento++;
                    return accion();
                }
                catch (MySqlException ex) when (EsErrorTransitorio(ex) && intento < maxReintentos)
                {
                    Thread.Sleep(intento * 300); // Espera progresiva: 300ms, 600ms...
                }
            }
        }

        private static bool EsErrorTransitorio(MySqlException ex)
        {
            return ex.Number switch
            {
                1213 => true, // Deadlock found (dos PCs guardando la misma tabla a la vez)
                1205 => true, // Lock wait timeout exceeded
                1042 => true, // Unable to connect to server (micro-corte)
                0 => true,    // Connection timeout
                _ => false
            };
        }

        public bool ProbarRedYConexion(out string mensajeError)
        {
            mensajeError = string.Empty;
            try
            {
                using (MySqlConnection conexion = ObtenerConexion())
                {
                    conexion.Open();
                    return true;
                }
            }
            catch (MySqlException ex)
            {
                mensajeError = TraducirError(ex);
                return false;
            }
            catch (Exception ex)
            {
                mensajeError = "Error inesperado en la red física: " + ex.Message;
                return false;
            }
        }

        public static string TraducirError(MySqlException ex)
        {
            return ex.Number switch
            {
                1042 or 1043 => "No se puede alcanzar la PC Servidor. Verifique que esté encendida y los cables conectados al Switch.",
                1045 => "Acceso denegado. Verifique las credenciales en el archivo conexion.config.",
                1049 => "La base de datos 'db_carabobo' no existe en el servidor.",
                1062 => "Registro duplicado: la cédula o código ya existe en el sistema.",
                1205 => "El servidor tardó en responder porque otro usuario está guardando en la misma tabla. Intente de nuevo.",
                1213 => "Conflicto de concurrencia temporal. El sistema reintentó la operación.",
                1451 => "No se puede eliminar el registro porque tiene datos vinculados (notas o asignaciones).",
                1452 => "Referencia inválida (el grado, sección o período seleccionado no existe).",
                _ => $"Error de base de datos ({ex.Number}): {ex.Message}"
            };
        }
    }
}