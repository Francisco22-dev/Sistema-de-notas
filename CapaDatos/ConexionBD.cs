using System;
using System.Collections.Generic;
using System.IO;
using MySqlConnector;

namespace SistemaLiceo.Datos
{
    /// <summary>
    /// Fabrica de conexiones hacia la base de datos db_carabobo (MariaDB / MySQL).
    /// Los parametros se leen del archivo "conexion.config" que se copia junto al
    /// ejecutable; asi la PC de cada usuario puede apuntar al servidor del liceo
    /// sin recompilar el programa.
    /// </summary>
    public class ConexionBD
    {
        private const string ArchivoConfiguracion = "conexion.config";
        private static readonly object Candado = new object();
        private static string? _cadenaConexion;

        /// <summary>Cadena de conexion en uso. Se puede sobrescribir desde el arranque de la aplicacion.</summary>
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
                UserID = Obtener(valores, "Uid", "root"),
                Password = Obtener(valores, "Pwd", ""),

                ConnectionTimeout = 5,   // Si el switch falla, avisa en 5 segundos
                Keepalive = 10,          // Mantiene viva la conexion en la red local
                Pooling = true,
                MinimumPoolSize = 1,
                MaximumPoolSize = 50,
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

        /// <summary>Devuelve una conexion cerrada, lista para abrirse.</summary>
        public MySqlConnection ObtenerConexion()
        {
            return new MySqlConnection(CadenaConexion);
        }

        /// <summary>Devuelve una conexion ya abierta.</summary>
        public MySqlConnection AbrirConexion()
        {
            MySqlConnection conexion = new MySqlConnection(CadenaConexion);
            conexion.Open();
            return conexion;
        }

        /// <summary>
        /// Verifica la red y el servicio de MariaDB. Ideal al abrir la aplicacion.
        /// </summary>
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
                mensajeError = "Error inesperado en la red: " + ex.Message;
                return false;
            }
        }

        /// <summary>Convierte los errores mas comunes de MariaDB en mensajes entendibles.</summary>
        public static string TraducirError(MySqlException ex)
        {
            switch (ex.Number)
            {
                case 1042:
                case 1043:
                    return "No se puede alcanzar el servidor. Verifique que la PC principal este encendida y el cable de red conectado al switch.";
                case 1045:
                    return "Acceso denegado. Verifique el usuario y la contrasena de la base de datos en el archivo conexion.config.";
                case 1049:
                    return "La base de datos db_carabobo no existe en el servidor. Ejecute el script db_carabobo.sql.";
                case 1062:
                    return "Registro duplicado: la cedula o el codigo ya existen en el sistema.";
                case 1452:
                    return "Se intento guardar un registro con una referencia inexistente (grado, seccion, periodo o parroquia).";
                case 4025:
                case 3819:
                    return "Los datos no cumplen una regla de la base de datos. Si el estudiante nacio en Venezuela debe indicar la parroquia; si nacio en el extranjero no debe indicarla.";
                default:
                    return "Error de base de datos (" + ex.Number + "): " + ex.Message;
            }
        }
    }
}
