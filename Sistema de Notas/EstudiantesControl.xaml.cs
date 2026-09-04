using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using SistemaLiceo.Datos;
using SistemaLiceo.Negocio;

namespace SistemaLiceo.Presentacion
{
    public partial class EstudiantesControl : UserControl
    {
        private readonly EstudianteDatos _estudiantes = new EstudianteDatos();
        private readonly CatalogoDatos _catalogos = new CatalogoDatos();
        private readonly InscripcionNegocio _negocio = new InscripcionNegocio();
        private DataTable? _dtEstudiantes;
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
                _dtEstudiantes = _estudiantes.ObtenerEstudiantesActivos(periodoId);
                gridEstudiantes.ItemsSource = _dtEstudiantes.DefaultView;
                AplicarFiltroBusqueda();
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", "No se pudieron cargar los estudiantes: " + ex.Message, true);
            }
        }

        private void txtBuscarEstudiante_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltroBusqueda();
        }

        private void AplicarFiltroBusqueda()
        {
            if (_dtEstudiantes == null) return;

            string busqueda = txtBuscarEstudiante.Text.Trim().Replace("'", "''");

            if (string.IsNullOrWhiteSpace(busqueda))
            {
                _dtEstudiantes.DefaultView.RowFilter = string.Empty;
            }
            else
            {
                _dtEstudiantes.DefaultView.RowFilter =
                    $"Cedula LIKE '%{busqueda}%' OR [Cedula Escolar] LIKE '%{busqueda}%' OR Estudiante LIKE '%{busqueda}%'";
            }
        }

        private void cmbPeriodo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_cargando)
                CargarDatos();
        }

        private void btnActualizar_Click(object sender, RoutedEventArgs e)
        {
            txtBuscarEstudiante.Clear();
            CargarDatos();
        }

        private void btnNuevoEstudiante_Click(object sender, RoutedEventArgs e)
        {
            EstudianteForm ventana = new EstudianteForm();
            if (ventana.ShowDialog() == true)
                CargarDatos();
        }

        private int? ObtenerIdSeleccionado()
        {
            if (gridEstudiantes.SelectedItem is DataRowView fila)
            {
                if (fila.Row.Table.Columns.Contains("Codigo"))
                    return Convert.ToInt32(fila["Codigo"]);
            }

            Alerta.Mostrar("Selección Requerida", "Por favor seleccione un estudiante de la tabla.", true);
            return null;
        }

        private void btnEditarEstudiante_Click(object sender, RoutedEventArgs e)
        {
            int? estudianteId = ObtenerIdSeleccionado();
            if (estudianteId.HasValue)
            {
                EstudianteForm ventana = new EstudianteForm(estudianteId.Value);
                if (ventana.ShowDialog() == true)
                    CargarDatos();
            }
        }

        private void btnRetirarEstudiante_Click(object sender, RoutedEventArgs e)
        {
            int? estudianteId = ObtenerIdSeleccionado();
            if (!estudianteId.HasValue) return;

            MessageBoxResult resultado = MessageBox.Show(
                "¿Está seguro de que desea cambiar el estado del estudiante a 'Retirado'?",
                "Confirmar Retiro",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (resultado == MessageBoxResult.Yes)
            {
                try
                {
                    _negocio.RetirarEstudiante(estudianteId.Value);
                    Alerta.Mostrar("Éxito", "Estudiante retirado correctamente.", false);
                    CargarDatos();
                }
                catch (Exception ex)
                {
                    Alerta.Mostrar("Error", "No se pudo retirar el estudiante: " + ex.Message, true);
                }
            }
        }
    }
}