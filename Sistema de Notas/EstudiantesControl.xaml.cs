using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using SistemaLiceo.Datos;

namespace SistemaLiceo.Presentacion
{
    public partial class EstudiantesControl : UserControl
    {
        private readonly EstudianteDatos _estudiantes = new EstudianteDatos();
        private readonly CatalogoDatos _catalogos = new CatalogoDatos();
        private bool _cargando;

        public EstudiantesControl()
        {
            InitializeComponent();
            CargarPeriodos();
            CargarDatos();
        }

        private void CargarPeriodos()
        {
            try
            {
                _cargando = true;
                cmbPeriodo.ItemsSource = _catalogos.ListarPeriodosActivos();
                if (cmbPeriodo.Items.Count > 0)
                    cmbPeriodo.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", "No se pudieron cargar los períodos: " + ex.Message, true);
            }
            finally
            {
                _cargando = false;
            }
        }

        private void CargarDatos()
        {
            try
            {
                int periodoId = cmbPeriodo.SelectedValue is int valor ? valor : 0;
                DataTable estudiantes = _estudiantes.ObtenerEstudiantesActivos(periodoId);
                gridEstudiantes.ItemsSource = estudiantes.DefaultView;
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", "No se pudieron cargar los estudiantes: " + ex.Message, true);
            }
        }

        private void cmbPeriodo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_cargando)
                CargarDatos();
        }

        private void btnActualizar_Click(object sender, RoutedEventArgs e) => CargarDatos();

        private void btnNuevoEstudiante_Click(object sender, RoutedEventArgs e)
        {
            EstudianteForm ventanaInscripcion = new EstudianteForm();
            ventanaInscripcion.ShowDialog();
            CargarDatos();
        }
    }
}
