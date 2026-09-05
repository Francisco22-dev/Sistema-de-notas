using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Entidades;
using SistemaLiceo.Datos;
using SistemaLiceo.Negocio;

namespace SistemaLiceo.Presentacion
{
    public partial class NotasControl : UserControl
    {
        private readonly CatalogoDatos _catalogos = new CatalogoDatos();
        private readonly ProfesorDatos _profesores = new ProfesorDatos();
        private readonly NotaDatos _notas = new NotaDatos();

        private List<FilaPlanillaNotasDto> _filasPlanilla = new List<FilaPlanillaNotasDto>();
        private List<MateriaProfesorPeriodo> _todasLasCargas = new List<MateriaProfesorPeriodo>();

        public NotasControl()
        {
            InitializeComponent();
            CargarFiltrosIniciales();
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
                Alerta.Mostrar("Error", "No se pudieron cargar los catálogos: " + ex.Message, true);
            }
        }

        private void cmbPeriodo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbPeriodo.SelectedValue is int periodoId)
            {
                try
                {
                    _todasLasCargas = _profesores.ListarCargasAcademicas(periodoId);
                    ActualizarComboDocenteMateria();
                }
                catch (Exception ex)
                {
                    Alerta.Mostrar("Error", "Error al cargar asignaciones: " + ex.Message, true);
                }
            }
        }

        private void FiltroCascada_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ActualizarComboDocenteMateria();
        }

        private void ActualizarComboDocenteMateria()
        {
            if (cmbGrado.SelectedValue == null || cmbSeccion.SelectedValue == null || _todasLasCargas == null) return;

            string gradoNombre = cmbGrado.Text;
            string seccionNombre = cmbSeccion.Text;

            // Filtra por Año y Sección seleccionados
            var filtradas = _todasLasCargas
                .Where(c => c.Grado == gradoNombre && c.Seccion == seccionNombre)
                .Select(c => new
                {
                    c.Id,
                    Display = $"{c.Materia} (Docente: {c.Docente})"
                }).ToList();

            cmbDocenteMateria.ItemsSource = filtradas;
            if (filtradas.Count > 0) cmbDocenteMateria.SelectedIndex = 0;
        }

        private void Configuracion_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;

            decimal[] ponderaciones = ObtenerPonderacionesActuales();
            FilaPlanillaNotasDto.Ponderaciones = ponderaciones;

            // Actualizar encabezados
            colEval1.Header = $"{txtEval1.Text}\n({ponderaciones[0]}%)";
            colEval2.Header = $"{txtEval2.Text}\n({ponderaciones[1]}%)";
            colEval3.Header = $"{txtEval3.Text}\n({ponderaciones[2]}%)";
            colEval4.Header = $"{txtEval4.Text}\n({ponderaciones[3]}%)";
            colEval5.Header = $"{txtEval5.Text}\n({ponderaciones[4]}%)";
            colEval6.Header = $"{txtEval6.Text}\n({ponderaciones[5]}%)";

            decimal total = ponderaciones.Sum();
            lblTotalPorcentaje.Text = $"Total Ponderación: {total}%";
            lblTotalPorcentaje.Foreground = total == 100 ? Brushes.Green : Brushes.Red;

            // Recalcular la grilla en vivo
            foreach (var fila in _filasPlanilla)
            {
                fila.Recalcular();
            }
        }

        private decimal[] ObtenerPonderacionesActuales()
        {
            return new decimal[]
            {
                ADecimal(txtPorc1.Text, 20),
                ADecimal(txtPorc2.Text, 20),
                ADecimal(txtPorc3.Text, 20),
                ADecimal(txtPorc4.Text, 20),
                ADecimal(txtPorc5.Text, 10),
                ADecimal(txtPorc6.Text, 10)
            };
        }

        private static decimal ADecimal(string texto, decimal porDefecto)
        {
            return decimal.TryParse(texto.Trim(), out decimal val) ? val : porDefecto;
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
            if (cmbDocenteMateria.SelectedValue == null)
            {
                Alerta.Mostrar("Advertencia", "Seleccione el año, sección y la materia asignada.", true);
                return;
            }

            int mppId = Convert.ToInt32(cmbDocenteMateria.SelectedValue);
            string lapsoBD = ObtenerLapsoBD();

            try
            {
                Configuracion_TextChanged(sender, null!);
                _filasPlanilla = _notas.ObtenerPlanillaLapso(mppId, lapsoBD);
                gridPlanilla.ItemsSource = null;
                gridPlanilla.ItemsSource = _filasPlanilla;

                if (_filasPlanilla.Count == 0)
                    Alerta.Mostrar("Información", "No hay estudiantes inscritos en esta sección.", false);
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
                Alerta.Mostrar("Advertencia", "No hay calificaciones en pantalla para guardar.", true);
                return;
            }

            int mppId = Convert.ToInt32(cmbDocenteMateria.SelectedValue);
            string lapsoBD = ObtenerLapsoBD();

            string[] nombres = new string[]
            {
                txtEval1.Text.Trim(), txtEval2.Text.Trim(), txtEval3.Text.Trim(),
                txtEval4.Text.Trim(), txtEval5.Text.Trim(), txtEval6.Text.Trim()
            };

            decimal[] ponderaciones = ObtenerPonderacionesActuales();

            try
            {
                _notas.GuardarPlanillaCompleta(mppId, lapsoBD, nombres, ponderaciones, _filasPlanilla);
                AuditoriaDatos.Registrar(SesionActual.IdUsuario, "Calificaciones", $"Guardó calificaciones de {lapsoBD} en asignación ID {mppId}");
                Alerta.Mostrar("Éxito", "¡Planilla y ponderaciones guardadas con éxito!", false);
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

            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                // Usa el generador FlowDocument del reporte anterior
                IDocumentPaginatorSource dps = GenerarDocumentoImpresion();
                printDialog.PrintDocument(dps.DocumentPaginator, "Planilla de Calificaciones");
            }
        }

        private FlowDocument GenerarDocumentoImpresion()
        {
            FlowDocument doc = new FlowDocument
            {
                PagePadding = new Thickness(30),
                PageWidth = 850,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11
            };

            Paragraph pHead = new Paragraph { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 8) };
            pHead.Inlines.Add(new Bold(new Run("UNIDAD EDUCATIVA COLEGIO NUESTRA SEÑORA DE LOURDES\n")) { FontSize = 14 });
            pHead.Inlines.Add(new Run("Departamento de Control de Estudios y Evaluación\n") { FontSize = 11 });
            pHead.Inlines.Add(new Run($"Año Escolar: {cmbPeriodo.Text}   |   {cmbGrado.Text} \"{cmbSeccion.Text}\"   |   {cmbLapso.Text}\n") { FontSize = 10 });
            pHead.Inlines.Add(new Bold(new Run($"Área de Formación: {cmbDocenteMateria.Text}\n")) { FontSize = 11 });
            doc.Blocks.Add(pHead);

            Table tabla = new Table { CellSpacing = 0, BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) };
            tabla.Columns.Add(new TableColumn { Width = new GridLength(30) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(85) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(200) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(50) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(50) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(50) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(50) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(50) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(50) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(60) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(60) });

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
            TableCell cell = new TableCell(p) { BorderBrush = Brushes.Black, BorderThickness = new Thickness(0.5) };
            if (fondo != null) cell.Background = fondo;
            return cell;
        }
    }
}