using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using Entidades;
using MySqlConnector;

namespace SistemaLiceo.Datos
{
    public class NotasCertificadasHistoricasDatos
    {
        private readonly ConexionBD _conexion = new ConexionBD();

        public void Guardar(NotaCertificadaHistorica item)
        {
            string jsonMaterias = JsonSerializer.Serialize(item.DatosPensum);

            const string consulta = @"
                INSERT INTO notas_certificadas_historicas 
                    (cedula, nombres, apellidos, fecha_nacimiento, pais_nacimiento, estado_nacimiento,
                     municipio_nacimiento, plantel_egreso, promedio_general, observaciones, materias_json)
                VALUES 
                    (@cedula, @nombres, @apellidos, @fnac, @pais, @estado,
                     @mun, @plantel, @prom, @obs, @json)
                ON DUPLICATE KEY UPDATE
                    nombres = VALUES(nombres),
                    apellidos = VALUES(apellidos),
                    fecha_nacimiento = VALUES(fecha_nacimiento),
                    pais_nacimiento = VALUES(pais_nacimiento),
                    estado_nacimiento = VALUES(estado_nacimiento),
                    municipio_nacimiento = VALUES(municipio_nacimiento),
                    plantel_egreso = VALUES(plantel_egreso),
                    promedio_general = VALUES(promedio_general),
                    observaciones = VALUES(observaciones),
                    materias_json = VALUES(materias_json);";

            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@cedula", item.Cedula.Trim().ToUpper());
                comando.Parameters.AddWithValue("@nombres", item.Nombres.Trim().ToUpper());
                comando.Parameters.AddWithValue("@apellidos", item.Apellidos.Trim().ToUpper());
                comando.Parameters.AddWithValue("@fnac", (object?)item.FechaNacimiento ?? DBNull.Value);
                comando.Parameters.AddWithValue("@pais", item.PaisNacimiento.Trim().ToUpper());
                comando.Parameters.AddWithValue("@estado", item.EstadoNacimiento.Trim().ToUpper());
                comando.Parameters.AddWithValue("@mun", item.MunicipioNacimiento.Trim().ToUpper());
                comando.Parameters.AddWithValue("@plantel", item.PlantelEgreso.Trim().ToUpper());
                comando.Parameters.AddWithValue("@prom", item.PromedioGeneral);
                comando.Parameters.AddWithValue("@obs", PersonaDatos.Nulo(item.Observaciones));
                comando.Parameters.AddWithValue("@json", jsonMaterias);

                comando.ExecuteNonQuery();
            }
        }

        public NotaCertificadaHistorica? BuscarPorCedula(string cedula)
        {
            // Limpia la cédula para comparar solo los números
            string soloDigitos = new string(cedula.Where(char.IsDigit).ToArray());
            if (string.IsNullOrWhiteSpace(soloDigitos)) return null;

            const string consulta = @"
        SELECT id, cedula, nombres, apellidos, fecha_nacimiento, pais_nacimiento,
               estado_nacimiento, municipio_nacimiento, plantel_egreso, promedio_general,
               observaciones, materias_json
        FROM notas_certificadas_historicas
        WHERE REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(cedula, 'V-', ''), 'E-', ''), '.', ''), '-', ''), ' ', '') = @digitos
           OR cedula LIKE @busqueda
        LIMIT 1;";

            using (MySqlConnection conexion = _conexion.AbrirConexion())
            using (MySqlCommand comando = new MySqlCommand(consulta, conexion))
            {
                comando.Parameters.AddWithValue("@digitos", soloDigitos);
                comando.Parameters.AddWithValue("@busqueda", $"%{soloDigitos}%");

                using (MySqlDataReader lector = comando.ExecuteReader())
                {
                    if (!lector.Read()) return null;

                    string json = lector.GetString("materias_json");
                    var pensum = JsonSerializer.Deserialize<CertificacionEstudianteCompletaDto>(json) ?? new();

                    return new NotaCertificadaHistorica
                    {
                        Id = lector.GetInt32("id"),
                        Cedula = lector.GetString("cedula"),
                        Nombres = lector.GetString("nombres"),
                        Apellidos = lector.GetString("apellidos"),
                        FechaNacimiento = lector.IsDBNull(lector.GetOrdinal("fecha_nacimiento")) ? null : lector.GetDateTime("fecha_nacimiento"),
                        PaisNacimiento = lector.GetString("pais_nacimiento"),
                        EstadoNacimiento = lector.GetString("estado_nacimiento"),
                        MunicipioNacimiento = lector.GetString("municipio_nacimiento"),
                        PlantelEgreso = lector.GetString("plantel_egreso"),
                        PromedioGeneral = lector.GetDecimal("promedio_general"),
                        Observaciones = lector.IsDBNull(lector.GetOrdinal("observaciones")) ? null : lector.GetString("observaciones"),
                        DatosPensum = pensum
                    };
                }
            }
        }
    }
}