using Entidades;
using SistemaLiceo.Datos;
using SistemaLiceo.Negocio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace SistemaLiceo.Presentacion
{
    public partial class NotasControl : UserControl
    {
        private readonly CatalogoDatos _catalogos = new CatalogoDatos();
        private readonly ProfesorDatos _profesores = new ProfesorDatos();
        private readonly NotaDatos _notas = new NotaDatos();

        private List<FilaPlanillaNotasDto> _filasPlanilla = new List<FilaPlanillaNotasDto>();
        private List<MateriaProfesorPeriodo> _cargasActuales = new List<MateriaProfesorPeriodo>();

        public NotasControl()
        {
            InitializeComponent();
            ActualizarEncabezadosColumnas();
            CargarPeriodos();
        }

        private void CargarPeriodos()
        {
            try
            {
                cmbPeriodo.ItemsSource = _catalogos.ListarPeriodosActivos();
                if (cmbPeriodo.Items.Count > 0) cmbPeriodo.SelectedIndex = 0;
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
                    _cargasActuales = _profesores.ListarCargasAcademicas(periodoId);
                    cmbCargaAcademica.ItemsSource = _cargasActuales.Select(c => new
                    {
                        c.Id,
                        Display = $"{c.Grado} \"{c.Seccion}\"  |  Área: {c.Materia}  |  Docente: {c.Docente}"
                    }).ToList();

                    cmbCargaAcademica.DisplayMemberPath = "Display";
                    if (cmbCargaAcademica.Items.Count > 0) cmbCargaAcademica.SelectedIndex = 0;
                }
                catch (Exception ex)
                {
                    Alerta.Mostrar("Error", "Error al cargar asignaciones: " + ex.Message, true);
                }
            }
        }

        private void cmbLapso_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && cmbCargaAcademica.SelectedValue != null)
            {
                btnCargarPlanilla_Click(sender, e);
            }
        }

        private void NombresEvaluaciones_TextChanged(object sender, TextChangedEventArgs e)
        {
            ActualizarEncabezadosColumnas();
        }

        private void ActualizarEncabezadosColumnas()
        {
            if (colEval1 == null) return;
            colEval1.Header = string.IsNullOrWhiteSpace(txtEval1.Text) ? "Act. 1" : txtEval1.Text;
            colEval2.Header = string.IsNullOrWhiteSpace(txtEval2.Text) ? "Act. 2" : txtEval2.Text;
            colEval3.Header = string.IsNullOrWhiteSpace(txtEval3.Text) ? "Act. 3" : txtEval3.Text;
            colEval4.Header = string.IsNullOrWhiteSpace(txtEval4.Text) ? "Act. 4" : txtEval4.Text;
            colEval5.Header = string.IsNullOrWhiteSpace(txtEval5.Text) ? "Act. 5" : txtEval5.Text;
            colEval6.Header = string.IsNullOrWhiteSpace(txtEval6.Text) ? "Act. 6" : txtEval6.Text;
        }

        private string ObtenerLapsoBD()
        {
            string seleccionado = ((ComboBoxItem)cmbLapso.SelectedItem).Content.ToString() ?? "I Momento";
            return seleccionado switch
            {
                "I Momento" => "1er lapso",
                "II Momento" => "2do lapso",
                "III Momento" => "3er lapso",
                _ => "Reparacion"
            };
        }

        private void btnCargarPlanilla_Click(object sender, RoutedEventArgs e)
        {
            if (cmbCargaAcademica.SelectedValue == null)
            {
                Alerta.Mostrar("Advertencia", "Seleccione una asignación académica.", true);
                return;
            }

            int mppId = Convert.ToInt32(cmbCargaAcademica.SelectedValue);
            string lapsoBD = ObtenerLapsoBD();

            try
            {
                _filasPlanilla = _notas.ObtenerPlanillaLapso(mppId, lapsoBD);
                gridPlanilla.ItemsSource = null;
                gridPlanilla.ItemsSource = _filasPlanilla;

                if (_filasPlanilla.Count == 0)
                {
                    Alerta.Mostrar("Información", "No hay estudiantes matriculados en esta sección.", false);
                }
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", "Error al consultar la planilla: " + ex.Message, true);
            }
        }

        private void btnGuardarPlanilla_Click(object sender, RoutedEventArgs e)
        {
            if (_filasPlanilla == null || _filasPlanilla.Count == 0)
            {
                Alerta.Mostrar("Advertencia", "No hay estudiantes en la planilla para guardar.", true);
                return;
            }

            int mppId = Convert.ToInt32(cmbCargaAcademica.SelectedValue);
            string lapsoBD = ObtenerLapsoBD();

            string[] nombres = new string[]
            {
                txtEval1.Text.Trim(), txtEval2.Text.Trim(), txtEval3.Text.Trim(),
                txtEval4.Text.Trim(), txtEval5.Text.Trim(), txtEval6.Text.Trim()
            };

            decimal[] ponderaciones = new decimal[] { 20, 20, 20, 20, 10, 10 };

            try
            {
                _notas.GuardarPlanillaCompleta(mppId, lapsoBD, nombres, ponderaciones, _filasPlanilla);
                AuditoriaDatos.Registrar(SesionActual.IdUsuario, "Calificaciones", $"Guardó planilla de notas {lapsoBD} en asignación {mppId}");
                Alerta.Mostrar("Éxito", "¡Planilla de calificaciones guardada con éxito!", false);
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", "Error al guardar calificaciones: " + ex.Message, true);
            }
        }

        private void btnImprimirPlanilla_Click(object sender, RoutedEventArgs e)
        {
            if (_filasPlanilla == null || _filasPlanilla.Count == 0)
            {
                Alerta.Mostrar("Advertencia", "Cargue primero la planilla para poder imprimirla.", true);
                return;
            }

            try
            {
                int mppId = Convert.ToInt32(cmbCargaAcademica.SelectedValue);
                MateriaProfesorPeriodo? carga = _cargasActuales.FirstOrDefault(c => c.Id == mppId);
                string momento = ((ComboBoxItem)cmbLapso.SelectedItem).Content.ToString() ?? "I Momento";
                string periodo = cmbPeriodo.Text;

                FlowDocument doc = GenerarDocumentoImpresion(carga, momento, periodo);

                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    IDocumentPaginatorSource dps = doc;
                    printDialog.PrintDocument(dps.DocumentPaginator, "Planilla de Calificaciones");
                }
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", "Error al preparar la impresión: " + ex.Message, true);
            }
        }

        private FlowDocument GenerarDocumentoImpresion(MateriaProfesorPeriodo? carga, string momento, string periodo)
        {
            FlowDocument doc = new FlowDocument
            {
                PagePadding = new Thickness(30),
                PageWidth = 850,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11
            };

            // Encabezado institucional
            Paragraph pHead = new Paragraph { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 8) };
            pHead.Inlines.Add(new Bold(new Run("UNIDAD EDUCATIVA COLEGIO NUESTRA SEÑORA DE LOURDES\n")) { FontSize = 14 });
            pHead.Inlines.Add(new Run("Departamento de Control de Estudios y Evaluación\n") { FontSize = 11 });
            pHead.Inlines.Add(new Run($"Código Plantel: S0207D0814 - CARABOBO.   |   Año Escolar: {periodo}\n") { FontSize = 10 });
            pHead.Inlines.Add(new Bold(new Run($"Docente: {carga?.Docente}   |   Área de Formación: {carga?.Materia}   |   {carga?.Grado} \"{carga?.Seccion}\"   |   {momento}\n")) { FontSize = 11 });
            doc.Blocks.Add(pHead);

            // Tabla idéntica a la imagen
            Table tabla = new Table { CellSpacing = 0, BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) };
            tabla.Columns.Add(new TableColumn { Width = new GridLength(30) });  // Nº
            tabla.Columns.Add(new TableColumn { Width = new GridLength(85) });  // Cédula
            tabla.Columns.Add(new TableColumn { Width = new GridLength(200) }); // Nombre
            tabla.Columns.Add(new TableColumn { Width = new GridLength(50) });  // E1
            tabla.Columns.Add(new TableColumn { Width = new GridLength(50) });  // E2
            tabla.Columns.Add(new TableColumn { Width = new GridLength(50) });  // E3
            tabla.Columns.Add(new TableColumn { Width = new GridLength(50) });  // E4
            tabla.Columns.Add(new TableColumn { Width = new GridLength(50) });  // E5
            tabla.Columns.Add(new TableColumn { Width = new GridLength(50) });  // E6
            tabla.Columns.Add(new TableColumn { Width = new GridLength(60) });  // Sumatoria
            tabla.Columns.Add(new TableColumn { Width = new GridLength(60) });  // Definitiva

            TableRowGroup grupo = new TableRowGroup();
            TableRow cab = new TableRow { Background = Brushes.LightGray };
            cab.Cells.Add(CrearCelda("№", true));
            cab.Cells.Add(CrearCelda("Cédula", true));
            cab.Cells.Add(CrearCelda("Apellidos y Nombres", true));
            cab.Cells.Add(CrearCelda(txtEval1.Text, true));
            cab.Cells.Add(CrearCelda(txtEval2.Text, true));
            cab.Cells.Add(CrearCelda(txtEval3.Text, true));
            cab.Cells.Add(CrearCelda(txtEval4.Text, true));
            cab.Cells.Add(CrearCelda(txtEval5.Text, true));
            cab.Cells.Add(CrearCelda(txtEval6.Text, true));
            cab.Cells.Add(CrearCelda("Sumatoria", true));
            cab.Cells.Add(CrearCelda("Definitiva", true));
            grupo.Rows.Add(cab);

            foreach (var r in _filasPlanilla)
            {
                TableRow fila = new TableRow();
                fila.Cells.Add(CrearCelda(r.NroLista.ToString()));
                fila.Cells.Add(CrearCelda(r.Cedula));
                fila.Cells.Add(CrearCelda(r.ApellidosYNombres, false, TextAlignment.Left));
                fila.Cells.Add(CrearCelda(r.Eval1?.ToString("N0") ?? ""));
                fila.Cells.Add(CrearCelda(r.Eval2?.ToString("N0") ?? ""));
                fila.Cells.Add(CrearCelda(r.Eval3?.ToString("N0") ?? ""));
                fila.Cells.Add(CrearCelda(r.Eval4?.ToString("N0") ?? ""));
                fila.Cells.Add(CrearCelda(r.Eval5?.ToString("N0") ?? ""));
                fila.Cells.Add(CrearCelda(r.Eval6?.ToString("N0") ?? ""));
                fila.Cells.Add(CrearCelda(r.Sumatoria.ToString("N2")));
                fila.Cells.Add(CrearCelda(r.Definitiva.ToString(), true, TextAlignment.Center, Brushes.LightYellow));
                grupo.Rows.Add(fila);
            }

            tabla.RowGroups.Add(grupo);
            doc.Blocks.Add(tabla);
            return doc;
        }

        private static TableCell CrearCelda(string texto, bool esBold = false, TextAlignment align = TextAlignment.Center, Brush? fondo = null)
        {
            Paragraph p = new Paragraph(new Run(texto)) { Margin = new Thickness(3), TextAlignment = align };
            if (esBold) p.FontWeight = FontWeights.Bold;
            TableCell cell = new TableCell(p)
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(0.5)
            };
            if (fondo != null) cell.Background = fondo;
            return cell;
        }
    }
}