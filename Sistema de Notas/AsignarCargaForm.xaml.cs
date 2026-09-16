using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Entidades;
using SistemaLiceo.Datos;
using SistemaLiceo.Negocio;

namespace SistemaLiceo.Presentacion
{
    public partial class AsignarCargaForm : Window
    {
        private readonly CatalogoDatos _catalogos = new();
        private readonly ProfesorDatos _profesores = new();

        public ObservableCollection<ProfesorComboDto> ListaProfesoresCombo { get; set; } = new();
        private List<AsignacionMateriaDocenteDto> _materiasPensum = new();

        public AsignarCargaForm()
        {
            InitializeComponent();
            DataContext = this;
            CargarFiltrosIniciales();
            CargarListaProfesores();
        }

        private void CargarFiltrosIniciales()
        {
            try
            {
                cmbPeriodo.ItemsSource = _catalogos.ListarPeriodosActivos();
                cmbGrado.ItemsSource = _catalogos.ListarGrados();
                cmbSeccion.ItemsSource = _catalogos.ListarSecciones();

                if (cmbPeriodo.Items.Count > 0) cmbPeriodo.SelectedIndex = 0;
                if (cmbGrado.Items.Count > 0) cmbGrado.SelectedIndex = 0;
                if (cmbSeccion.Items.Count > 0) cmbSeccion.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", "Error al cargar catálogos: " + ex.Message, true);
            }
        }

        private void CargarListaProfesores()
        {
            try
            {
                ListaProfesoresCombo.Clear();
                // Opción para desasignar o dejar vacante
                ListaProfesoresCombo.Add(new ProfesorComboDto { Id = 0, NombreCompleto = "-- Sin Asignar --" });

                DataTable dt = _profesores.ListarActivos();
                foreach (DataRow r in dt.Rows)
                {
                    ListaProfesoresCombo.Add(new ProfesorComboDto
                    {
                        Id = Convert.ToInt32(r["Codigo"]),
                        NombreCompleto = $"{r["Profesor"]} ({r["Cedula"]})"
                    });
                }
            }
            catch { }
        }

        private void Filtro_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
                CargarPensumSeccion();
        }

        private void btnCargar_Click(object sender, RoutedEventArgs e) => CargarPensumSeccion();

        private void CargarPensumSeccion()
        {
            if (cmbPeriodo.SelectedValue == null || cmbGrado.SelectedValue == null || cmbSeccion.SelectedValue == null)
                return;

            int periodoId = Convert.ToInt32(cmbPeriodo.SelectedValue);
            int gradoId = Convert.ToInt32(cmbGrado.SelectedValue);
            int seccionId = Convert.ToInt32(cmbSeccion.SelectedValue);

            try
            {
                _materiasPensum = _profesores.ObtenerMateriasPensumSeccion(gradoId, seccionId, periodoId);
                gridMateriasSeccion.ItemsSource = null;
                gridMateriasSeccion.ItemsSource = _materiasPensum;
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", "Error al consultar las materias: " + ex.Message, true);
            }
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (_materiasPensum == null || _materiasPensum.Count == 0)
            {
                Alerta.Mostrar("Advertencia", "No hay materias en pantalla para asignar.", true);
                return;
            }

            int periodoId = Convert.ToInt32(cmbPeriodo.SelectedValue);
            int gradoId = Convert.ToInt32(cmbGrado.SelectedValue);
            int seccionId = Convert.ToInt32(cmbSeccion.SelectedValue);

            try
            {
                _profesores.GuardarAsignacionCompletaSeccion(gradoId, seccionId, periodoId, _materiasPensum);
                AuditoriaDatos.Registrar(SesionActual.IdUsuario, "Carga Académica", $"Guardó la distribución de docentes para {cmbGrado.Text} \"{cmbSeccion.Text}\" ({_materiasPensum.Count(x => x.ProfesorId > 0)} materias asignadas)");

                Alerta.Mostrar("Éxito", $"¡Carga académica de {cmbGrado.Text} \"{cmbSeccion.Text}\" guardada con éxito!", false);
                CargarPensumSeccion();
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", "Error al guardar la asignación: " + ex.Message, true);
            }
        }

        private void btnCerrar_Click(object sender, RoutedEventArgs e) => Close();
    }
}