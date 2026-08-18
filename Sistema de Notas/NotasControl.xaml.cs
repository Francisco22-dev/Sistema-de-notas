using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Entidades;
using SistemaLiceo.Datos;

namespace SistemaLiceo.Presentacion
{
    public partial class NotasControl : UserControl
    {
        private readonly CatalogoDatos _catalogos = new CatalogoDatos();
        private readonly ProfesorDatos _profesores = new ProfesorDatos();
        private readonly NotaDatos _notas = new NotaDatos();
        private List<CalificacionEstudianteDto> _listaAlumnos = new List<CalificacionEstudianteDto>();

        public NotasControl()
        {
            InitializeComponent();
            CargarPeriodos();
        }

        private void CargarPeriodos()
        {
            try
            {
                cmbPeriodo.ItemsSource = _catalogos.ListarPeriodosActivos();
                if (cmbPeriodo.Items.Count > 0)
                    cmbPeriodo.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", "Error al cargar períodos: " + ex.Message, true);
            }
        }

        private void cmbPeriodo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbPeriodo.SelectedValue is int periodoId)
            {
                try
                {
                    List<MateriaProfesorPeriodo> cargas = _profesores.ListarCargasAcademicas(periodoId);
                    cmbCargaAcademica.ItemsSource = cargas.Select(c => new
                    {
                        c.Id,
                        Display = $"{c.Grado} \"{c.Seccion}\" - {c.Materia} ({c.Docente})"
                    }).ToList();

                    cmbCargaAcademica.DisplayMemberPath = "Display";
                    if (cmbCargaAcademica.Items.Count > 0)
                        cmbCargaAcademica.SelectedIndex = 0;
                }
                catch (Exception ex)
                {
                    Alerta.Mostrar("Error", "Error al cargar asignaciones: " + ex.Message, true);
                }
            }
        }

        private void btnCargarEstudiantes_Click(object sender, RoutedEventArgs e)
        {
            if (cmbCargaAcademica.SelectedValue == null)
            {
                Alerta.Mostrar("Advertencia", "Seleccione una carga académica.", true);
                return;
            }

            string evaluacion = txtDescripcionEvaluacion.Text.Trim();
            if (string.IsNullOrWhiteSpace(evaluacion))
            {
                Alerta.Mostrar("Advertencia", "Indique la descripción de la evaluación.", true);
                return;
            }

            int mppId = Convert.ToInt32(cmbCargaAcademica.SelectedValue);
            string lapso = ((ComboBoxItem)cmbLapso.SelectedItem).Content.ToString() ?? "1er lapso";

            try
            {
                _listaAlumnos = _notas.ObtenerEstudiantesParaCargaNotas(mppId, lapso, evaluacion);
                gridNotas.ItemsSource = null;
                gridNotas.ItemsSource = _listaAlumnos;

                if (_listaAlumnos.Count == 0)
                    Alerta.Mostrar("Información", "No se encontraron estudiantes inscritos en esta sección.", false);
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", "Error al consultar estudiantes: " + ex.Message, true);
            }
        }

        private void btnGuardarNotas_Click(object sender, RoutedEventArgs e)
        {
            if (_listaAlumnos == null || _listaAlumnos.Count == 0)
            {
                Alerta.Mostrar("Advertencia", "No hay calificaciones para guardar.", true);
                return;
            }

            if (!int.TryParse(txtPorcentaje.Text.Trim(), out int porcentaje) || porcentaje <= 0 || porcentaje > 100)
            {
                Alerta.Mostrar("Advertencia", "El porcentaje de la evaluación debe ser un número entero entre 1 y 100.", true);
                return;
            }

            // Validar rango de notas 0-20
            foreach (var item in _listaAlumnos)
            {
                if (item.NotaEvaluacion < 0 || item.NotaEvaluacion > 20)
                {
                    Alerta.Mostrar("Nota Inválida", $"La calificación de {item.NombreCompleto} debe estar entre 0 y 20 puntos.", true);
                    return;
                }
            }

            int mppId = Convert.ToInt32(cmbCargaAcademica.SelectedValue);
            string lapso = ((ComboBoxItem)cmbLapso.SelectedItem).Content.ToString() ?? "1er lapso";
            string evaluacion = txtDescripcionEvaluacion.Text.Trim();

            try
            {
                _notas.GuardarNotasSeccionMasiva(mppId, lapso, evaluacion, porcentaje, _listaAlumnos);
                Alerta.Mostrar("Éxito", "¡Calificaciones guardadas y promedios actualizados con éxito!", false);
                btnCargarEstudiantes_Click(sender, e); // Refresca las notas y las definitivas calculadas
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", ex.Message, true);
            }
        }
    }
}