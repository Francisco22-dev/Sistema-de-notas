using System;
using System.Data;
using System.Windows;
using Entidades;
using MySqlConnector;
using SistemaLiceo.Datos;

namespace SistemaLiceo.Presentacion
{
    public partial class AsignarCargaForm : Window
    {
        private readonly CatalogoDatos _catalogos = new CatalogoDatos();
        private readonly ProfesorDatos _profesores = new ProfesorDatos();
        private readonly ConexionBD _conexion = new ConexionBD();

        public AsignarCargaForm()
        {
            InitializeComponent();
            CargarDatos();
        }

        private void CargarDatos()
        {
            try
            {
                cmbPeriodo.ItemsSource = _catalogos.ListarPeriodosActivos();
                cmbMateria.ItemsSource = _catalogos.ListarMaterias();
                cmbGrado.ItemsSource = _catalogos.ListarGrados();
                cmbSeccion.ItemsSource = _catalogos.ListarSecciones();

                DataTable dtProf = _profesores.ListarActivos();
                cmbProfesor.ItemsSource = dtProf.DefaultView;

                if (cmbPeriodo.Items.Count > 0) cmbPeriodo.SelectedIndex = 0;
                if (cmbMateria.Items.Count > 0) cmbMateria.SelectedIndex = 0;
                if (cmbGrado.Items.Count > 0) cmbGrado.SelectedIndex = 0;
                if (cmbSeccion.Items.Count > 0) cmbSeccion.SelectedIndex = 0;
                if (dtProf.Rows.Count > 0) cmbProfesor.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", "Error al cargar listas: " + ex.Message, true);
            }
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (cmbPeriodo.SelectedValue == null || cmbProfesor.SelectedValue == null ||
                cmbMateria.SelectedValue == null || cmbGrado.SelectedValue == null || cmbSeccion.SelectedValue == null)
            {
                Alerta.Mostrar("Advertencia", "Seleccione todos los campos requeridos.", true);
                return;
            }

            try
            {
                int periodoId = Convert.ToInt32(cmbPeriodo.SelectedValue);
                int profesorId = Convert.ToInt32(cmbProfesor.SelectedValue);
                int materiaId = Convert.ToInt32(cmbMateria.SelectedValue);
                int gradoId = Convert.ToInt32(cmbGrado.SelectedValue);
                int seccionId = Convert.ToInt32(cmbSeccion.SelectedValue);

                int gradoSeccionId = _catalogos.ObtenerOCrearGradoSeccion(gradoId, seccionId);
                int materiaProfesorId = _profesores.AsignarMateriaAProfesor(profesorId, materiaId);
                int gradoMateriaId = ObtenerOCrearGradoMateria(gradoId, materiaId);

                _profesores.AsignarMateriaSeccionPeriodo(gradoSeccionId, gradoMateriaId, materiaProfesorId, periodoId);

                Alerta.Mostrar("Éxito", "Carga académica asignada correctamente.", false);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", ex.Message, true);
            }
        }

        private int ObtenerOCrearGradoMateria(int gradoId, int materiaId)
        {
            using (MySqlConnection conexion = _conexion.AbrirConexion())
            {
                const string busqueda = "SELECT id FROM GRADO_MATERIA WHERE grado_id = @g AND materia_id = @m LIMIT 1;";
                using (MySqlCommand cmd = new MySqlCommand(busqueda, conexion))
                {
                    cmd.Parameters.AddWithValue("@g", gradoId);
                    cmd.Parameters.AddWithValue("@m", materiaId);
                    object? res = cmd.ExecuteScalar();
                    if (res != null && res != DBNull.Value)
                        return Convert.ToInt32(res);
                }

                const string insercion = @"INSERT INTO GRADO_MATERIA (grado_id, materia_id) VALUES (@g, @m);
                                           SELECT LAST_INSERT_ID();";
                using (MySqlCommand cmd = new MySqlCommand(insercion, conexion))
                {
                    cmd.Parameters.AddWithValue("@g", gradoId);
                    cmd.Parameters.AddWithValue("@m", materiaId);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e) => Close();
    }
}