using System;
using System.Collections.Generic;
using Entidades;
using MySqlConnector;

namespace SistemaLiceo.Datos
{
    /// <summary>Consultas de solo lectura sobre los catalogos de db_carabobo.</summary>
    public class CatalogoDatos
    {
        private readonly ConexionBD _conexion = new ConexionBD();

        public List<Catalogo> ListarPaises() =>
            Listar("SELECT id, nombre FROM PAIS ORDER BY nombre;");

        public List<Catalogo> ListarEstados(int paisId = Pais.VenezuelaId) =>
            Listar("SELECT id, nombre FROM ESTADO WHERE pais_id = @p ORDER BY nombre;", ("@p", paisId));

        public List<Catalogo> ListarCiudades(int estadoId) =>
            Listar("SELECT id, nombre FROM CIUDAD WHERE estado_id = @e ORDER BY nombre;", ("@e", estadoId));

        public List<Catalogo> ListarMunicipios(int estadoId) =>
            Listar("SELECT id, nombre FROM MUNICIPIO WHERE estado_id = @e ORDER BY nombre;", ("@e", estadoId));

        public List<Catalogo> ListarParroquias(int municipioId) =>
            Listar("SELECT id, nombre FROM PARROQUIA WHERE municipio_id = @m ORDER BY nombre;", ("@m", municipioId));

        public List<Catalogo> ListarGrados() =>
            Listar("SELECT id, nombre FROM GRADO ORDER BY id;");

        public List<Catalogo> ListarSecciones() =>
            Listar("SELECT id, nombre FROM SECCION ORDER BY nombre;");

        public List<Catalogo> ListarMaterias() =>
            Listar("SELECT id, nombre FROM MATERIA ORDER BY nombre;");

        public List<Catalogo> ListarPeriodosActivos() =>
            Listar("SELECT id, nombre FROM PERIODO_ACADEMICO WHERE ESTADO = 'Activo' ORDER BY nombre;");

        /// <summary>Todas las combinaciones grado + seccion registradas en GRADO_SECCION.</summary>
        public List<GradoSeccion> ListarGradoSecciones()
        {
            List<GradoSeccion> lista = new List<GradoSeccion>();

            const string consulta = @"
                SELECT gs.id, gs.grado_id, gs.seccion_id, g.nombre AS grado, s.nombre AS seccion
                FROM GRADO_SECCION gs
                INNER JOIN GRADO g ON g.id = gs.grado_id
                INNER JOIN SECCION s ON s.id = gs.seccion_id
                ORDER BY g.id, s.nombre;";

            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            using (MySqlDataReader lector = comando.ExecuteReader())
            {
                while (lector.Read())
                {
                    lista.Add(new GradoSeccion
                    {
                        Id = lector.GetInt32("id"),
                        GradoId = lector.GetInt32("grado_id"),
                        SeccionId = lector.GetInt32("seccion_id"),
                        GradoNombre = lector.GetString("grado"),
                        SeccionNombre = lector.GetString("seccion")
                    });
                }
            }

            return lista;
        }

        /// <summary>
        /// Devuelve el id de la combinacion grado + seccion; la crea si el liceo aun no la tiene registrada.
        /// </summary>
        public int ObtenerOCrearGradoSeccion(int gradoId, int seccionId)
        {
            using (MySqlConnection conexion = _conexion.AbrirConexion())
            {
                const string busqueda = "SELECT id FROM GRADO_SECCION WHERE grado_id = @g AND seccion_id = @s LIMIT 1;";
                using (MySqlCommand comando = new MySqlCommand(busqueda, conexion))
                {
                    comando.Parameters.AddWithValue("@g", gradoId);
                    comando.Parameters.AddWithValue("@s", seccionId);
                    object? resultado = comando.ExecuteScalar();
                    if (resultado != null && resultado != DBNull.Value)
                        return Convert.ToInt32(resultado);
                }

                const string insercion = @"INSERT INTO GRADO_SECCION (grado_id, seccion_id) VALUES (@g, @s);
                                           SELECT LAST_INSERT_ID();";
                using (MySqlCommand comando = new MySqlCommand(insercion, conexion))
                {
                    comando.Parameters.AddWithValue("@g", gradoId);
                    comando.Parameters.AddWithValue("@s", seccionId);
                    return Convert.ToInt32(comando.ExecuteScalar());
                }
            }
        }

        private List<Catalogo> Listar(string consulta, params (string Nombre, object Valor)[] parametros)
        {
            List<Catalogo> lista = new List<Catalogo>();

            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                foreach ((string nombre, object valor) in parametros)
                    comando.Parameters.AddWithValue(nombre, valor);

                using (MySqlDataReader lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                        lista.Add(new Catalogo(lector.GetInt32(0), lector.GetString(1)));
                }
            }

            return lista;
        }
        public int RegistrarMateria(string nombre)
        {
            using (MySqlConnection conexion = _conexion.AbrirConexion())
            {
                const string consulta = "INSERT INTO MATERIA (nombre) VALUES (@nombre); SELECT LAST_INSERT_ID();";
                using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@nombre", nombre.Trim());
                    return Convert.ToInt32(comando.ExecuteScalar());
                }
            }
        }

        public void ActualizarMateria(int id, string nombre)
        {
            using (MySqlConnection conexion = _conexion.AbrirConexion())
            {
                const string consulta = "UPDATE MATERIA SET nombre = @nombre WHERE id = @id;";
                using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@id", id);
                    comando.Parameters.AddWithValue("@nombre", nombre.Trim());
                    comando.ExecuteNonQuery();
                }
            }
        }

        public void EliminarMateria(int id)
        {
            using (MySqlConnection conexion = _conexion.AbrirConexion())
            {
                try
                {
                    const string consulta = "DELETE FROM MATERIA WHERE id = @id;";
                    using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
                    {
                        comando.Parameters.AddWithValue("@id", id);
                        comando.ExecuteNonQuery();
                    }
                }
                catch (MySqlException ex)
                {
                    if (ex.Number == 1451) // Foreign Key Constraint
                        throw new Exception("No se puede eliminar la materia porque ya está asignada a un pensum académico o a un docente.");
                    throw new Exception(ConexionBD.TraducirError(ex), ex);
                }
            }
        }

        public bool ExisteMateria(string nombre, int idExcluir = 0)
        {
            using (MySqlConnection conexion = _conexion.AbrirConexion())
            {
                const string consulta = "SELECT 1 FROM MATERIA WHERE nombre = @nombre AND id <> @id LIMIT 1;";
                using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
                {
                    comando.Parameters.AddWithValue("@nombre", nombre.Trim());
                    comando.Parameters.AddWithValue("@id", idExcluir);
                    return comando.ExecuteScalar() != null;
                }
            }
        }
    }
}
