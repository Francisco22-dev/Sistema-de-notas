using System;
using Entidades;
using MySqlConnector;

namespace SistemaLiceo.Datos
{
    public class ConfiguracionPlantelDatos
    {
        private readonly ConexionBD _conexion = new ConexionBD();

        public ConfiguracionPlantel Obtener()
        {
            const string consulta = @"
                SELECT id, eponimo, codigo_plantel, codigo_plan_estudio, denominacion_plan,
                       direccion, telefono, municipio, entidad_federal, cdcee,
                       director_nombre, director_cedula
                FROM configuracion_plantel
                WHERE id = 1 LIMIT 1;";

            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            using (MySqlDataReader lector = comando.ExecuteReader())
            {
                if (!lector.Read()) return new ConfiguracionPlantel();

                return new ConfiguracionPlantel
                {
                    Id = lector.GetInt32("id"),
                    Eponimo = lector.GetString("eponimo"),
                    CodigoPlantel = lector.GetString("codigo_plantel"),
                    CodigoPlanEstudio = lector.GetString("codigo_plan_estudio"),
                    DenominacionPlan = lector.GetString("denominacion_plan"),
                    Direccion = lector.GetString("direccion"),
                    Telefono = lector.GetString("telefono"),
                    Municipio = lector.GetString("municipio"),
                    EntidadFederal = lector.GetString("entidad_federal"),
                    Cdcee = lector.GetString("cdcee"),
                    DirectorNombre = lector.GetString("director_nombre"),
                    DirectorCedula = lector.GetString("director_cedula")
                };
            }
        }

        public void Guardar(ConfiguracionPlantel conf)
        {
            const string consulta = @"
                INSERT INTO configuracion_plantel (id, eponimo, codigo_plantel, codigo_plan_estudio, denominacion_plan,
                                                  direccion, telefono, municipio, entidad_federal, cdcee,
                                                  director_nombre, director_cedula)
                VALUES (1, @eponimo, @codigoPlantel, @codigoPlan, @denominacion,
                        @direccion, @telefono, @municipio, @entidad, @cdcee,
                        @directorNom, @directorCed)
                ON DUPLICATE KEY UPDATE
                    eponimo = VALUES(eponimo),
                    codigo_plantel = VALUES(codigo_plantel),
                    codigo_plan_estudio = VALUES(codigo_plan_estudio),
                    denominacion_plan = VALUES(denominacion_plan),
                    direccion = VALUES(direccion),
                    telefono = VALUES(telefono),
                    municipio = VALUES(municipio),
                    entidad_federal = VALUES(entidad_federal),
                    cdcee = VALUES(cdcee),
                    director_nombre = VALUES(director_nombre),
                    director_cedula = VALUES(director_cedula);";

            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@eponimo", conf.Eponimo.Trim().ToUpper());
                comando.Parameters.AddWithValue("@codigoPlantel", conf.CodigoPlantel.Trim().ToUpper());
                comando.Parameters.AddWithValue("@codigoPlan", conf.CodigoPlanEstudio.Trim().ToUpper());
                comando.Parameters.AddWithValue("@denominacion", conf.DenominacionPlan.Trim().ToUpper());
                comando.Parameters.AddWithValue("@direccion", conf.Direccion.Trim().ToUpper());
                comando.Parameters.AddWithValue("@telefono", conf.Telefono.Trim());
                comando.Parameters.AddWithValue("@municipio", conf.Municipio.Trim().ToUpper());
                comando.Parameters.AddWithValue("@entidad", conf.EntidadFederal.Trim().ToUpper());
                comando.Parameters.AddWithValue("@cdcee", conf.Cdcee.Trim().ToUpper());
                comando.Parameters.AddWithValue("@directorNom", conf.DirectorNombre.Trim().ToUpper());
                comando.Parameters.AddWithValue("@directorCed", conf.DirectorCedula.Trim().ToUpper());

                comando.ExecuteNonQuery();
            }
        }
    }
}