using Entidades;
using Microsoft.Win32;
using SistemaLiceo.Datos;
using SistemaLiceo.Negocio;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Xps;

namespace SistemaLiceo.Presentacion
{
    public partial class ReportesControl : UserControl
    {
        private const string EponimoLiceo = "UNIDAD EDUCATIVA CARABOBO";
        private const string CodigoDea = "CÓDIGO DEA: T0311D0814";
        private const string UbicacionPlantel = "PARROQUIA SAN JOSÉ, VALENCIA - ESTADO CARABOBO";

        private readonly CatalogoDatos _catalogos = new CatalogoDatos();
        private readonly EstudianteDatos _estudiantes = new EstudianteDatos();
        private readonly ReportesDatos _reportes = new ReportesDatos();
        private List<EstudianteItemCombo> _listaEstudiantesCompleta = new List<EstudianteItemCombo>();

        private class EstudianteItemCombo
        {
            public int Codigo { get; set; }
            public string Cedula { get; set; } = string.Empty;
            public string Estudiante { get; set; } = string.Empty;
            public string Display => $"{Estudiante} ({Cedula})";
        }

        public ReportesControl()
        {
            InitializeComponent();
            CargarFiltros();
        }

        private void CargarFiltros()
        {
            try
            {
                cmbPeriodo.ItemsSource = _catalogos.ListarPeriodosActivos();
                if (cmbPeriodo.Items.Count > 0) cmbPeriodo.SelectedIndex = 0;

                List<GradoSeccion> gs = _catalogos.ListarGradoSecciones();
                cmbGradoSeccion.ItemsSource = gs.Select(g => new { g.Id, Display = $"{g.GradoNombre} \"{g.SeccionNombre}\"" }).ToList();
                if (cmbGradoSeccion.Items.Count > 0) cmbGradoSeccion.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", "No se pudieron cargar los filtros: " + ex.Message, true);
            }
        }

        private void cmbTipoReporte_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            bool esGlobal = cmbTipoReporte.SelectedIndex >= 4; // Nómina y SAZE son globales
            panelBuscarCedula.Visibility = esGlobal ? Visibility.Collapsed : Visibility.Visible;
            panelEstudiante.Visibility = esGlobal ? Visibility.Collapsed : Visibility.Visible;
        }

        private void Filtro_SelectionChanged(object sender, SelectionChangedEventArgs e) => CargarEstudiantesSeccion();
        private void cmbGradoSeccion_SelectionChanged(object sender, SelectionChangedEventArgs e) => CargarEstudiantesSeccion();

        private void cmbFormatoCedula_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Si ya hay un documento generado en pantalla, refrescar con el nuevo formato automáticamente
            if (IsLoaded && docViewer.Document != null)
            {
                btnGenerar_Click(sender, e);
            }
        }

        private void CargarEstudiantesSeccion()
        {
            if (cmbPeriodo.SelectedValue == null) return;
            try
            {
                int periodoId = Convert.ToInt32(cmbPeriodo.SelectedValue);
                DataTable dt = _estudiantes.ObtenerEstudiantesActivos(periodoId);

                _listaEstudiantesCompleta = dt.AsEnumerable().Select(r => new EstudianteItemCombo
                {
                    Codigo = r.Field<int>("Codigo"),
                    Cedula = r.Field<string>("Cedula") ?? string.Empty,
                    Estudiante = r.Field<string>("Estudiante") ?? string.Empty
                }).ToList();

                cmbEstudiantes.ItemsSource = _listaEstudiantesCompleta;
                if (_listaEstudiantesCompleta.Count > 0) cmbEstudiantes.SelectedIndex = 0;
            }
            catch { }
        }

        private void txtBuscarCedula_TextChanged(object sender, TextChangedEventArgs e)
        {
            string busqueda = txtBuscarCedula.Text.Trim();

            if (string.IsNullOrWhiteSpace(busqueda))
            {
                cmbEstudiantes.ItemsSource = _listaEstudiantesCompleta;
                if (_listaEstudiantesCompleta.Count > 0) cmbEstudiantes.SelectedIndex = 0;
            }
            else
            {
                var filtrados = _listaEstudiantesCompleta
                    .Where(x => x.Cedula.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                                x.Estudiante.Contains(busqueda, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                cmbEstudiantes.ItemsSource = filtrados;
                if (filtrados.Count > 0) cmbEstudiantes.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Aplica el formato de cédula seleccionado: 99999999, 99.999.999 o 99-999-999.
        /// </summary>
        public static string FormatearCedula(string cedulaOriginal, string estiloFormato, bool incluirNacionalidad = true)
        {
            if (string.IsNullOrWhiteSpace(cedulaOriginal)) return "S/C";

            string nac = "V";
            string texto = cedulaOriginal.Trim().ToUpper();

            if (texto.StartsWith("V-") || texto.StartsWith("V"))
            {
                nac = "V";
                texto = texto.Replace("V-", "").Replace("V", "").Trim();
            }
            else if (texto.StartsWith("E-") || texto.StartsWith("E"))
            {
                nac = "E";
                texto = texto.Replace("E-", "").Replace("E", "").Trim();
            }

            string soloDigitos = new string(texto.Where(char.IsDigit).ToArray());
            if (soloDigitos.Length == 0) return cedulaOriginal;

            string numeroFinal;
            if (long.TryParse(soloDigitos, out long numero))
            {
                switch (estiloFormato)
                {
                    case "Puntos": // 99.999.999
                        numeroFinal = string.Format(new CultureInfo("de-DE"), "{0:N0}", numero);
                        break;
                    case "Guiones": // 99-999-999
                        numeroFinal = string.Format(new CultureInfo("de-DE"), "{0:N0}", numero).Replace('.', '-');
                        break;
                    case "Plano": // 99999999
                    default:
                        numeroFinal = soloDigitos;
                        break;
                }
            }
            else
            {
                numeroFinal = soloDigitos;
            }

            return incluirNacionalidad ? $"{nac}-{numeroFinal}" : numeroFinal;
        }

        private string ObtenerEstiloCedulaSeleccionado()
        {
            if (cmbFormatoCedula.SelectedItem is ComboBoxItem item && item.Tag != null)
                return item.Tag.ToString() ?? "Puntos";
            return "Puntos";
        }

        private void btnGenerar_Click(object sender, RoutedEventArgs e)
        {
            if (cmbPeriodo.SelectedValue == null || cmbGradoSeccion.SelectedValue == null)
            {
                Alerta.Mostrar("Advertencia", "Seleccione el período académico y la sección.", true);
                return;
            }

            int periodoId = Convert.ToInt32(cmbPeriodo.SelectedValue);
            int gradoSeccionId = Convert.ToInt32(cmbGradoSeccion.SelectedValue);
            int tipo = cmbTipoReporte.SelectedIndex;
            string estiloCedula = ObtenerEstiloCedulaSeleccionado();

            if (tipo < 4 && cmbEstudiantes.SelectedValue == null)
            {
                Alerta.Mostrar("Advertencia", "Seleccione un estudiante para emitir este documento.", true);
                return;
            }

            int estudianteId = tipo < 4 ? Convert.ToInt32(cmbEstudiantes.SelectedValue) : 0;

            FlowDocument doc = tipo switch
            {
                0 => GenerarDocumentoConstancia(estudianteId, periodoId, "CONSTANCIA DE ESTUDIO", false, estiloCedula),
                1 => GenerarDocumentoConstancia(estudianteId, periodoId, "CONSTANCIA DE BUENA CONDUCTA", true, estiloCedula),
                2 => GenerarDocumentoNotasCertificadas(estudianteId, periodoId, estiloCedula),
                3 => GenerarDocumentoBoleta(estudianteId, periodoId, estiloCedula),
                4 => GenerarDocumentoNomina(gradoSeccionId, periodoId, estiloCedula),
                5 => GenerarDocumentoSazeMatricula(gradoSeccionId, periodoId, estiloCedula),
                6 => GenerarDocumentoSazeRendimiento(gradoSeccionId, periodoId),
                _ => CrearDocumentoBase()
            };

            docViewer.Document = doc;
        }

        private FlowDocument GenerarDocumentoConstancia(int estudianteId, int periodoId, string titulo, bool esConducta, string estiloCedula)
        {
            ConstanciaEstudioDto? datos = _reportes.ObtenerDatosConstancia(estudianteId, periodoId);
            FlowDocument doc = CrearDocumentoBase();
            if (datos == null) return DocumentoVacio(doc);

            string cedulaFormateada = FormatearCedula(datos.Cedula, estiloCedula);

            AgregarMembrete(doc);

            Paragraph pTitulo = new Paragraph(new Run(titulo))
            {
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 20, 0, 30)
            };
            doc.Blocks.Add(pTitulo);

            string cuerpo = esConducta
                ? $"Quien suscribe, la Dirección de la institución {EponimoLiceo}, hace constar por medio de la presente que el/la estudiante {datos.EstudianteNombreCompleto}, titular de la Cédula de Identidad Nº {cedulaFormateada}, cursante del {datos.Grado}, Sección \"{datos.Seccion}\", durante el año escolar {datos.Periodo}, ha demostrado una EXCELENTE CONDUCTA, acatando las normas de convivencia escolar y demostrando respeto y colaboración."
                : $"Quien suscribe, la Dirección de la institución {EponimoLiceo}, hace constar por medio de la presente que el/la estudiante {datos.EstudianteNombreCompleto}, titular de la Cédula de Identidad Nº {cedulaFormateada} (Cédula Escolar Nº {datos.CedulaEscolar}), se encuentra debidamente inscrito(a) en este plantel cursando el {datos.Grado}, Sección \"{datos.Seccion}\" de Educación {datos.NivelAcademico}, durante el Año Escolar {datos.Periodo}.";

            Paragraph pCuerpo = new Paragraph(new Run(cuerpo))
            {
                FontSize = 14,
                TextAlignment = TextAlignment.Justify,
                LineHeight = 24,
                Margin = new Thickness(0, 0, 0, 40)
            };
            doc.Blocks.Add(pCuerpo);

            string fechaHoy = DateTime.Now.ToString("dd 'días del mes de' MMMM 'de' yyyy", new CultureInfo("es-ES"));
            Paragraph pFecha = new Paragraph(new Run($"Constancia que se expide a petición de la parte interesada, en la ciudad de Valencia, a los {fechaHoy}."))
            {
                FontSize = 13,
                TextAlignment = TextAlignment.Justify,
                Margin = new Thickness(0, 0, 0, 80)
            };
            doc.Blocks.Add(pFecha);

            AgregarFirmas(doc);
            return doc;
        }

        private FlowDocument GenerarDocumentoNotasCertificadas(int estudianteId, int periodoId, string estiloCedula)
        {
            ConfiguracionPlantelDatos confDatos = new ConfiguracionPlantelDatos();
            ConfiguracionPlantel conf = confDatos.Obtener();
            CertificacionEstudianteCompletaDto? est = _reportes.ObtenerCertificacionOficialCompleta(estudianteId);

            FlowDocument doc = new FlowDocument
            {
                PagePadding = new Thickness(25),
                FontFamily = new FontFamily("Arial"),
                FontSize = 9.5,
                PageWidth = 840
            };

            if (est == null) return DocumentoVacio(doc);

            string cedulaEst = FormatearCedula(est.Cedula, estiloCedula);

            // ================= ENCABEZADO SUPERIOR =================
            Table tHeader = new Table { Margin = new Thickness(0, 0, 0, 4) };
            tHeader.Columns.Add(new TableColumn { Width = new GridLength(280) });
            tHeader.Columns.Add(new TableColumn { Width = new GridLength(510) });

            TableRowGroup grpHead = new TableRowGroup();
            TableRow rHead = new TableRow();

            Paragraph pLogos = new Paragraph(new Bold(new Run("REPÚBLICA BOLIVARIANA DE VENEZUELA\nMINISTERIO DEL PODER POPULAR PARA LA\n")) { FontSize = 8.5 });
            pLogos.Inlines.Add(new Bold(new Run("EDUCACIÓN")) { FontSize = 14 });
            rHead.Cells.Add(new TableCell(pLogos));

            Paragraph pTitOficial = new Paragraph { TextAlignment = TextAlignment.Right, LineHeight = 13 };
            pTitOficial.Inlines.Add(new Bold(new Run("CERTIFICACIÓN DE CALIFICACIONES EMG\n")) { FontSize = 11 });
            pTitOficial.Inlines.Add(new Run($"I. Plan de Estudio: {conf.DenominacionPlan}     Código: {conf.CodigoPlanEstudio}\n") { FontSize = 8.5 });
            pTitOficial.Inlines.Add(new Run($"Lugar y Fecha de Expedición: {conf.EntidadFederal}, {DateTime.Now.ToString("dd 'DE' MMMM 'DE' yyyy", new CultureInfo("es-ES")).ToUpper()}\n") { FontSize = 8.5 });
            rHead.Cells.Add(new TableCell(pTitOficial));

            grpHead.Rows.Add(rHead);
            tHeader.RowGroups.Add(grpHead);
            doc.Blocks.Add(tHeader);

            // ================= II. DATOS DE LA INSTITUCIÓN =================
            Table tInst = CrearTablaMarco();
            tInst.Columns.Add(new TableColumn { Width = new GridLength(140) });
            tInst.Columns.Add(new TableColumn { Width = new GridLength(380) });
            tInst.Columns.Add(new TableColumn { Width = new GridLength(270) });

            TableRowGroup grpInst = new TableRowGroup();
            TableRow rInstTitle = new TableRow { Background = Brushes.WhiteSmoke };
            TableCell cInstTitle = new TableCell(new Paragraph(new Bold(new Run("II. Datos de la Institución Educativa o Centro de Desarrollo de la Calidad Educativa Estatal (CDCEE) que Emite la Certificación:"))) { Margin = new Thickness(2) }) { ColumnSpan = 3 };
            rInstTitle.Cells.Add(cInstTitle);
            grpInst.Rows.Add(rInstTitle);

            TableRow rInst1 = new TableRow();
            rInst1.Cells.Add(CrearCeldaTexto($"Código: {conf.CodigoPlantel}"));
            rInst1.Cells.Add(CrearCeldaTexto($"Denominación y Epónimo: {conf.Eponimo}"));
            rInst1.Cells.Add(CrearCeldaTexto($"Teléfono: {conf.Telefono}"));
            grpInst.Rows.Add(rInst1);

            TableRow rInst2 = new TableRow();
            rInst2.Cells.Add(CrearCeldaTexto($"Municipio: {conf.Municipio}"));
            rInst2.Cells.Add(CrearCeldaTexto($"Dirección: {conf.Direccion}"));
            rInst2.Cells.Add(CrearCeldaTexto($"Entidad Federal: {conf.EntidadFederal}   |   CDCEE: {conf.Cdcee}"));
            grpInst.Rows.Add(rInst2);

            tInst.RowGroups.Add(grpInst);
            doc.Blocks.Add(tInst);

            // ================= III. DATOS DE IDENTIFICACIÓN DEL ESTUDIANTE =================
            Table tEst = CrearTablaMarco();
            tEst.Columns.Add(new TableColumn { Width = new GridLength(260) });
            tEst.Columns.Add(new TableColumn { Width = new GridLength(260) });
            tEst.Columns.Add(new TableColumn { Width = new GridLength(270) });

            TableRowGroup grpEst = new TableRowGroup();
            TableRow rEstTitle = new TableRow { Background = Brushes.WhiteSmoke };
            rEstTitle.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("III. Datos de Identificación del Estudiante:"))) { Margin = new Thickness(2) }) { ColumnSpan = 3 });
            grpEst.Rows.Add(rEstTitle);

            TableRow rEst1 = new TableRow();
            rEst1.Cells.Add(CrearCeldaTexto($"Cédula de Identidad: {cedulaEst}"));
            rEst1.Cells.Add(CrearCeldaTexto($"Apellidos: {est.Apellidos}"));
            rEst1.Cells.Add(CrearCeldaTexto($"Nombres: {est.Nombres}"));
            grpEst.Rows.Add(rEst1);

            TableRow rEst2 = new TableRow();
            string fechaNac = est.FechaNacimiento.HasValue ? est.FechaNacimiento.Value.ToString("dd 'DE' MMMM 'DE' yyyy", new CultureInfo("es-ES")).ToUpper() : "S/F";
            rEst2.Cells.Add(CrearCeldaTexto($"Fecha de Nacimiento: {fechaNac}"));
            rEst2.Cells.Add(CrearCeldaTexto($"Lugar de Nacimiento: País: {est.PaisNacimiento}"));
            rEst2.Cells.Add(CrearCeldaTexto($"Estado: {est.EstadoNacimiento}   |   Municipio: {est.MunicipioNacimiento}"));
            grpEst.Rows.Add(rEst2);

            tEst.RowGroups.Add(grpEst);
            doc.Blocks.Add(tEst);

            // ================= IV. INSTITUCIONES DONDE CURSÓ ESTUDIOS =================
            Table tPlanteles = CrearTablaMarco();
            tPlanteles.Columns.Add(new TableColumn { Width = new GridLength(25) });
            tPlanteles.Columns.Add(new TableColumn { Width = new GridLength(450) });
            tPlanteles.Columns.Add(new TableColumn { Width = new GridLength(265) });
            tPlanteles.Columns.Add(new TableColumn { Width = new GridLength(50) });

            TableRowGroup grpPl = new TableRowGroup();
            TableRow rPlTitle = new TableRow { Background = Brushes.WhiteSmoke };
            rPlTitle.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("IV. Instituciones Educativas donde Cursó Estudios:"))) { Margin = new Thickness(2) }) { ColumnSpan = 4 });
            grpPl.Rows.Add(rPlTitle);

            TableRow rPlHead = new TableRow { Background = Brushes.LightGray };
            rPlHead.Cells.Add(CrearCeldaHeader("Nº"));
            rPlHead.Cells.Add(CrearCeldaHeader("Denominación y Epónimo de la Institución Educativa"));
            rPlHead.Cells.Add(CrearCeldaHeader("Localidad"));
            rPlHead.Cells.Add(CrearCeldaHeader("E.F."));
            grpPl.Rows.Add(rPlHead);

            TableRow rPl1 = new TableRow();
            rPl1.Cells.Add(CrearCeldaCentro("1"));
            rPl1.Cells.Add(CrearCeldaTexto(conf.Eponimo));
            rPl1.Cells.Add(CrearCeldaTexto(conf.Municipio));
            rPl1.Cells.Add(CrearCeldaCentro("CA"));
            grpPl.Rows.Add(rPl1);

            tPlanteles.RowGroups.Add(grpPl);
            doc.Blocks.Add(tPlanteles);

            // ================= V. PLAN DE ESTUDIO (1° A 5° AÑO EN COLUMNAS) =================
            Paragraph pV = new Paragraph(new Bold(new Run("V. Plan de Estudio:")) { FontSize = 9 }) { Margin = new Thickness(0, 3, 0, 2) };
            doc.Blocks.Add(pV);

            Table tPensumGrid = new Table { CellSpacing = 4, Margin = new Thickness(0) };
            tPensumGrid.Columns.Add(new TableColumn { Width = new GridLength(390) });
            tPensumGrid.Columns.Add(new TableColumn { Width = new GridLength(390) });

            TableRowGroup grpPensum = new TableRowGroup();

            // FILA 1: Primer Año (Izq) | Segundo Año (Der)
            TableRow rP1 = new TableRow();
            rP1.Cells.Add(new TableCell(GenerarTablaAnoEscolar("PRIMER AÑO", est.PrimerAno)));
            rP1.Cells.Add(new TableCell(GenerarTablaAnoEscolar("SEGUNDO AÑO", est.SegundoAno)));
            grpPensum.Rows.Add(rP1);

            // FILA 2: Tercer Año (Izq) | Cuarto Año (Der)
            TableRow rP2 = new TableRow();
            rP2.Cells.Add(new TableCell(GenerarTablaAnoEscolar("TERCER AÑO", est.TercerAno)));
            rP2.Cells.Add(new TableCell(GenerarTablaAnoEscolar("CUARTO AÑO", est.CuartoAno)));
            grpPensum.Rows.Add(rP2);

            // FILA 3: Quinto Año (Izq) | Grupos Estables y Orientación (Der)
            TableRow rP3 = new TableRow();
            rP3.Cells.Add(new TableCell(GenerarTablaAnoEscolar("QUINTO AÑO", est.QuintoAno)));
            rP3.Cells.Add(new TableCell(GenerarTablaGruposYOrientacion()));
            grpPensum.Rows.Add(rP3);

            tPensumGrid.RowGroups.Add(grpPensum);
            doc.Blocks.Add(tPensumGrid);

            // ================= VI. OBSERVACIONES (PROMEDIO GENERAL) =================
            Table tObs = CrearTablaMarco();
            tObs.Columns.Add(new TableColumn { Width = new GridLength(790) });
            TableRowGroup grpObs = new TableRowGroup();
            TableRow rObs = new TableRow();
            rObs.Cells.Add(CrearCeldaTexto($"VI. Observaciones:   Promedio General: {est.PromedioGeneral:N3}"));
            grpObs.Rows.Add(rObs);
            tObs.RowGroups.Add(grpObs);
            doc.Blocks.Add(tObs);

            // ================= VII Y VIII. FIRMAS, SELLOS Y TIMBRE FISCAL =================
            Table tFirmas = CrearTablaMarco();
            tFirmas.Columns.Add(new TableColumn { Width = new GridLength(395) });
            tFirmas.Columns.Add(new TableColumn { Width = new GridLength(395) });

            TableRowGroup grpF = new TableRowGroup();
            TableRow rFHead = new TableRow { Background = Brushes.WhiteSmoke };
            rFHead.Cells.Add(CrearCeldaHeader("VII. Institución Educativa (Director/a)"));
            rFHead.Cells.Add(CrearCeldaHeader("VIII. Centro de Desarrollo de la Calidad Educativa Estatal"));
            grpF.Rows.Add(rFHead);

            TableRow rFBody = new TableRow();
            Paragraph pF1 = new Paragraph { LineHeight = 14, Margin = new Thickness(4) };
            pF1.Inlines.Add(new Run($"Apellidos y Nombres: {conf.DirectorNombre}\n"));
            pF1.Inlines.Add(new Run($"Cédula de Identidad: {conf.DirectorCedula}\n\n\n"));
            pF1.Inlines.Add(new Run("Firma: ________________________   SELLO DEL PLANTEL\n"));
            pF1.Inlines.Add(new Run("Para efectos de su Validez Nacional") { FontSize = 8, Foreground = Brushes.Gray });
            rFBody.Cells.Add(new TableCell(pF1));

            Paragraph pF2 = new Paragraph { LineHeight = 14, Margin = new Thickness(4) };
            pF2.Inlines.Add(new Run("Director(a) de la Calidad Educativa:\n"));
            pF2.Inlines.Add(new Run("Cédula de Identidad: ____________________\n\n\n"));
            pF2.Inlines.Add(new Run("Firma: ________________________   SELLO DEL CDCEE\n"));
            pF2.Inlines.Add(new Run("Para efectos de su Validez Internacional") { FontSize = 8, Foreground = Brushes.Gray });
            rFBody.Cells.Add(new TableCell(pF2));

            grpF.Rows.Add(rFBody);

            TableRow rFiscal = new TableRow();
            TableCell cFiscal = new TableCell(new Paragraph(new Run("VALOR FISCAL: Para su validez legal y de acuerdo a la Ley de Timbre Fiscal al dorso de este documento se le debe colocar tres décimas de la Unidad Tributaria (0,3 U.T.)")) { FontSize = 7.5, TextAlignment = TextAlignment.Center, Margin = new Thickness(2) }) { ColumnSpan = 2 };
            rFiscal.Cells.Add(cFiscal);
            grpF.Rows.Add(rFiscal);

            tFirmas.RowGroups.Add(grpF);
            doc.Blocks.Add(tFirmas);

            return doc;
        }

        // ================= TABLAS AUXILIARES PARA EL PENSUM =================

        private static Table GenerarTablaAnoEscolar(string tituloAno, List<FilaMateriaPensumDto> materias)
        {
            Table t = new Table { CellSpacing = 0, BorderBrush = Brushes.Black, BorderThickness = new Thickness(0.5) };
            t.Columns.Add(new TableColumn { Width = new GridLength(170) }); // Materia
            t.Columns.Add(new TableColumn { Width = new GridLength(28) });  // Nº
            t.Columns.Add(new TableColumn { Width = new GridLength(75) });  // Letras
            t.Columns.Add(new TableColumn { Width = new GridLength(25) });  // T-E
            t.Columns.Add(new TableColumn { Width = new GridLength(52) });  // Mes/Año
            t.Columns.Add(new TableColumn { Width = new GridLength(20) });  // Inst

            TableRowGroup grp = new TableRowGroup();
            TableRow rTit = new TableRow { Background = Brushes.WhiteSmoke };
            rTit.Cells.Add(new TableCell(new Paragraph(new Bold(new Run(tituloAno))) { TextAlignment = TextAlignment.Center, Margin = new Thickness(1) }) { ColumnSpan = 6 });
            grp.Rows.Add(rTit);

            TableRow rH = new TableRow { Background = Brushes.LightGray, FontSize = 8 };
            rH.Cells.Add(CrearCeldaHeader("ÁREAS DE FORMACIÓN"));
            rH.Cells.Add(CrearCeldaHeader("Nº"));
            rH.Cells.Add(CrearCeldaHeader("LETRAS"));
            rH.Cells.Add(CrearCeldaHeader("T-E"));
            rH.Cells.Add(CrearCeldaHeader("FECHA"));
            rH.Cells.Add(CrearCeldaHeader("I"));
            grp.Rows.Add(rH);

            foreach (var m in materias)
            {
                TableRow r = new TableRow { FontSize = 8 };
                r.Cells.Add(CrearCeldaTexto(m.Materia));
                r.Cells.Add(CrearCeldaCentro(m.NotaNumero.HasValue ? m.NotaNumero.Value.ToString("D2") : "--"));
                r.Cells.Add(CrearCeldaCentro(m.NotaLetras));
                r.Cells.Add(CrearCeldaCentro(m.TipoEvaluacion));
                r.Cells.Add(CrearCeldaCentro(m.MesAno));
                r.Cells.Add(CrearCeldaCentro(m.InstitucionNro.ToString()));
                grp.Rows.Add(r);
            }

            t.RowGroups.Add(grp);

            // Retornamos la tabla directamente, ya que hereda de Block
            return t;
        }

        private static Table GenerarTablaGruposYOrientacion()
        {
            Table t = new Table { CellSpacing = 0, BorderBrush = Brushes.Black, BorderThickness = new Thickness(0.5) };
            t.Columns.Add(new TableColumn { Width = new GridLength(240) });
            t.Columns.Add(new TableColumn { Width = new GridLength(40) });
            t.Columns.Add(new TableColumn { Width = new GridLength(90) });

            TableRowGroup grp = new TableRowGroup();

            TableRow rTit = new TableRow { Background = Brushes.WhiteSmoke };
            rTit.Cells.Add(new TableCell(new Paragraph(new Bold(new Run("ÁREAS DE FORMACIÓN COMPLEMENTARIAS"))) { TextAlignment = TextAlignment.Center, Margin = new Thickness(1) }) { ColumnSpan = 3 });
            grp.Rows.Add(rTit);

            TableRow rH = new TableRow { Background = Brushes.LightGray, FontSize = 8 };
            rH.Cells.Add(CrearCeldaHeader("ÁREA DE FORMACIÓN"));
            rH.Cells.Add(CrearCeldaHeader("AÑO"));
            rH.Cells.Add(CrearCeldaHeader("LITERAL / GRUPO"));
            grp.Rows.Add(rH);

            for (int ano = 1; ano <= 5; ano++)
            {
                TableRow r = new TableRow { FontSize = 8 };
                r.Cells.Add(CrearCeldaTexto(ano == 1 ? "ORIENTACIÓN Y CONVIVENCIA" : "PARTICIPACIÓN EN GRUPOS DE CREACIÓN, RECREACIÓN Y PRODUCCIÓN"));
                r.Cells.Add(CrearCeldaCentro($"{ano}°"));
                r.Cells.Add(CrearCeldaCentro("A (PROTOCOLO)"));
                grp.Rows.Add(r);
            }

            t.RowGroups.Add(grp);

            // Retornamos la tabla directamente
            return t;
        }

        // ================= HELPERS DE CELDAS Y BORDES =================

        private static Table CrearTablaMarco()
        {
            return new Table
            {
                CellSpacing = 0,
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(0.5),
                Margin = new Thickness(0, 0, 0, 3)
            };
        }

        private static TableCell CrearCeldaTexto(string texto)
        {
            return new TableCell(new Paragraph(new Run(texto)) { Margin = new Thickness(3, 1, 3, 1) })
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(0.5)
            };
        }

        private static TableCell CrearCeldaCentro(string texto)
        {
            return new TableCell(new Paragraph(new Run(texto)) { Margin = new Thickness(1), TextAlignment = TextAlignment.Center })
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(0.5)
            };
        }

        private static TableCell CrearCeldaHeader(string texto)
        {
            return new TableCell(new Paragraph(new Bold(new Run(texto))) { Margin = new Thickness(1), TextAlignment = TextAlignment.Center })
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(0.5)
            };
        }

        private FlowDocument GenerarDocumentoBoleta(int estudianteId, int periodoId, string estiloCedula)
        {
            ConstanciaEstudioDto? est = _reportes.ObtenerDatosConstancia(estudianteId, periodoId);
            List<FilaBoletaDto> notas = _reportes.ObtenerBoletaNotas(estudianteId, periodoId);
            FlowDocument doc = CrearDocumentoBase();
            if (est == null) return DocumentoVacio(doc);

            string cedulaFormateada = FormatearCedula(est.Cedula, estiloCedula);

            AgregarMembrete(doc);

            Paragraph pTitulo = new Paragraph(new Run("BOLETÍN INFORMATIVO DE CALIFICACIONES"))
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 10, 0, 15)
            };
            doc.Blocks.Add(pTitulo);

            Paragraph pDatos = new Paragraph();
            pDatos.Inlines.Add(new Bold(new Run("Estudiante: ")));
            pDatos.Inlines.Add(new Run($"{est.EstudianteNombreCompleto}    "));
            pDatos.Inlines.Add(new Bold(new Run("Cédula: ")));
            pDatos.Inlines.Add(new Run($"{cedulaFormateada}\n"));
            pDatos.Inlines.Add(new Bold(new Run("Año/Grado: ")));
            pDatos.Inlines.Add(new Run($"{est.Grado} \"{est.Seccion}\"    "));
            pDatos.Inlines.Add(new Bold(new Run("Año Escolar: ")));
            pDatos.Inlines.Add(new Run($"{est.Periodo}"));
            pDatos.Margin = new Thickness(0, 0, 0, 15);
            doc.Blocks.Add(pDatos);

            Table tabla = new Table { CellSpacing = 0, BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) };
            tabla.Columns.Add(new TableColumn { Width = new GridLength(220) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(70) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(70) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(70) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(80) });

            TableRowGroup grupo = new TableRowGroup();
            TableRow cabecera = new TableRow { Background = Brushes.LightGray };
            cabecera.Cells.Add(CrearCelda("Asignatura", true));
            cabecera.Cells.Add(CrearCelda("1er Lapso", true));
            cabecera.Cells.Add(CrearCelda("2do Lapso", true));
            cabecera.Cells.Add(CrearCelda("3er Lapso", true));
            cabecera.Cells.Add(CrearCelda("Definitiva", true));
            grupo.Rows.Add(cabecera);

            foreach (var n in notas)
            {
                TableRow fila = new TableRow();
                fila.Cells.Add(CrearCelda(n.Materia));
                fila.Cells.Add(CrearCelda(n.NotaLapso1?.ToString("D2") ?? "-"));
                fila.Cells.Add(CrearCelda(n.NotaLapso2?.ToString("D2") ?? "-"));
                fila.Cells.Add(CrearCelda(n.NotaLapso3?.ToString("D2") ?? "-"));
                fila.Cells.Add(CrearCelda(n.NotaDefinitiva?.ToString("D2") ?? "-", true));
                grupo.Rows.Add(fila);
            }

            tabla.RowGroups.Add(grupo);
            doc.Blocks.Add(tabla);

            doc.Blocks.Add(new Paragraph(new Run("\n")));
            AgregarFirmas(doc);
            return doc;
        }

        private FlowDocument GenerarDocumentoNomina(int gradoSeccionId, int periodoId, string estiloCedula)
        {
            List<FilaNominaSeccionDto> lista = _reportes.ObtenerNominaSeccion(gradoSeccionId, periodoId);
            FlowDocument doc = CrearDocumentoBase();
            AgregarMembrete(doc);

            Paragraph pTitulo = new Paragraph(new Run("NÓMINA DE MATRÍCULA POR SECCIÓN"))
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 10, 0, 15)
            };
            doc.Blocks.Add(pTitulo);

            Table tabla = new Table { CellSpacing = 0, BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) };
            tabla.Columns.Add(new TableColumn { Width = new GridLength(35) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(110) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(200) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(45) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(150) });

            TableRowGroup grupo = new TableRowGroup();
            TableRow cab = new TableRow { Background = Brushes.LightGray };
            cab.Cells.Add(CrearCelda("Nº", true));
            cab.Cells.Add(CrearCelda("Cédula", true));
            cab.Cells.Add(CrearCelda("Estudiante", true));
            cab.Cells.Add(CrearCelda("Sexo", true));
            cab.Cells.Add(CrearCelda("Representante", true));
            grupo.Rows.Add(cab);

            foreach (var r in lista)
            {
                TableRow fila = new TableRow();
                fila.Cells.Add(CrearCelda(r.Numero.ToString()));
                fila.Cells.Add(CrearCelda(FormatearCedula(r.Cedula, estiloCedula)));
                fila.Cells.Add(CrearCelda(r.Estudiante));
                fila.Cells.Add(CrearCelda(r.Sexo));
                fila.Cells.Add(CrearCelda(r.Representante));
                grupo.Rows.Add(fila);
            }

            tabla.RowGroups.Add(grupo);
            doc.Blocks.Add(tabla);
            doc.Blocks.Add(new Paragraph(new Run("\n")));
            AgregarFirmas(doc);
            return doc;
        }
        private void btnExportarExcel_Click(object sender, RoutedEventArgs e)
        {
            if (cmbPeriodo.SelectedValue == null)
            {
                Alerta.Mostrar("Advertencia", "Seleccione el período académico a exportar.", true);
                return;
            }

            int periodoId = Convert.ToInt32(cmbPeriodo.SelectedValue);
            string periodoNombre = cmbPeriodo.Text;

            string nombreSugerido = $"SAZE_Matricula_Inicial_{periodoNombre.Replace("-", "_")}_{DateTime.Now:yyyyMMdd}.xlsx";

            SaveFileDialog sfd = new SaveFileDialog
            {
                Title = "Guardar Reporte SAZE en Formato Excel",
                Filter = "Libro de Excel (*.xlsx)|*.xlsx",
                FileName = nombreSugerido,
                DefaultExt = ".xlsx"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    _reportes.ExportarSazeMatriculaAExcel(periodoId, periodoNombre, sfd.FileName);
                    Alerta.Mostrar("Éxito", $"¡Reporte SAZE generado exitosamente en Excel!\n{Path.GetFileName(sfd.FileName)}", false);

                    // Pregunta opcional para abrir el archivo inmediatamente
                    MessageBoxResult res = MessageBox.Show(
                        "¿Desea abrir el archivo Excel generado ahora mismo?",
                        "Abrir Reporte",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (res == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = sfd.FileName,
                            UseShellExecute = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    Alerta.Mostrar("Error", "Error al exportar archivo Excel: " + ex.Message, true);
                }
            }
        }

        private FlowDocument GenerarDocumentoSazeMatricula(int gradoSeccionId, int periodoId, string estiloCedula)
        {
            ConfiguracionPlantelDatos confDatos = new ConfiguracionPlantelDatos();
            ConfiguracionPlantel conf = confDatos.Obtener();
            List<EstadisticaAnoSazeDto> estadisticas = _reportes.ObtenerEstadisticasSazeMatriculaPorEdades(periodoId);

            FlowDocument doc = new FlowDocument
            {
                PagePadding = new Thickness(25),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 9.5,
                PageWidth = 840
            };

            AgregarMembrete(doc);

            Paragraph pTitulo = new Paragraph(new Bold(new Run("FORMATO ESTADÍSTICO SAZE - RESUMEN DE MATRÍCULA INICIAL POR EDAD Y GÉNERO")))
            {
                FontSize = 12,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 5, 0, 10)
            };
            doc.Blocks.Add(pTitulo);

            // ================= 1. TABLA POR CADA AÑO ESCOLAR (1° A 5° AÑO) =================
            foreach (var ano in estadisticas)
            {
                Table tAno = CrearTablaMarco();
                int cantEdades = ano.DistribucionEdades.Count;

                // Columnas: Indicador (140px) + 2 subcolumnas (V y H) por cada edad + 3 columnas totales
                tAno.Columns.Add(new TableColumn { Width = new GridLength(160) });
                for (int i = 0; i < cantEdades; i++)
                {
                    tAno.Columns.Add(new TableColumn { Width = new GridLength(28) }); // V
                    tAno.Columns.Add(new TableColumn { Width = new GridLength(28) }); // H
                }
                tAno.Columns.Add(new TableColumn { Width = new GridLength(45) }); // Total V
                tAno.Columns.Add(new TableColumn { Width = new GridLength(45) }); // Total H
                tAno.Columns.Add(new TableColumn { Width = new GridLength(55) }); // Total General

                TableRowGroup grp = new TableRowGroup();

                // Fila Título del Año
                TableRow rTit = new TableRow { Background = Brushes.WhiteSmoke };
                int totalCols = 1 + (cantEdades * 2) + 3;
                TableCell cTit = new TableCell(new Paragraph(new Bold(new Run($"ESTADÍSTICA DE MATRÍCULA: {ano.Grado}"))) { Margin = new Thickness(3) })
                {
                    ColumnSpan = totalCols
                };
                rTit.Cells.Add(cTit);
                grp.Rows.Add(rTit);

                // Fila Encabezados de Edad
                TableRow rEdades = new TableRow { Background = Brushes.LightGray, FontSize = 8.5 };
                TableCell cRango = new TableCell(new Paragraph(new Bold(new Run("DISTRIBUCIÓN POR EDAD"))) { Margin = new Thickness(2) })
                {
                    RowSpan = 2
                };
                rEdades.Cells.Add(cRango);

                foreach (var edad in ano.DistribucionEdades.Keys)
                {
                    TableCell cEdad = new TableCell(new Paragraph(new Bold(new Run($"{edad} AÑOS"))) { TextAlignment = TextAlignment.Center, Margin = new Thickness(1) })
                    {
                        ColumnSpan = 2
                    };
                    rEdades.Cells.Add(cEdad);
                }

                TableCell cTotV = new TableCell(new Paragraph(new Bold(new Run("TOT. V"))) { TextAlignment = TextAlignment.Center, Margin = new Thickness(1) }) { RowSpan = 2 };
                TableCell cTotH = new TableCell(new Paragraph(new Bold(new Run("TOT. H"))) { TextAlignment = TextAlignment.Center, Margin = new Thickness(1) }) { RowSpan = 2 };
                TableCell cTotG = new TableCell(new Paragraph(new Bold(new Run("TOTAL"))) { TextAlignment = TextAlignment.Center, Margin = new Thickness(1) }) { RowSpan = 2 };
                rEdades.Cells.Add(cTotV);
                rEdades.Cells.Add(cTotH);
                rEdades.Cells.Add(cTotG);
                grp.Rows.Add(rEdades);

                // Fila Sub-encabezados V y H
                TableRow rSub = new TableRow { Background = Brushes.LightGray, FontSize = 8 };
                for (int i = 0; i < cantEdades; i++)
                {
                    rSub.Cells.Add(CrearCeldaCentro("V"));
                    rSub.Cells.Add(CrearCeldaCentro("H"));
                }
                grp.Rows.Add(rSub);

                // Fila de Valores de Edad
                TableRow rValores = new TableRow { FontSize = 8.5 };
                rValores.Cells.Add(CrearCeldaTexto("N° de Estudiantes"));
                foreach (var (m, f) in ano.DistribucionEdades.Values)
                {
                    rValores.Cells.Add(CrearCeldaCentro(m.ToString()));
                    rValores.Cells.Add(CrearCeldaCentro(f.ToString()));
                }
                rValores.Cells.Add(CrearCeldaCentro(ano.TotalVarones.ToString(), true));
                rValores.Cells.Add(CrearCeldaCentro(ano.TotalHembras.ToString(), true));
                rValores.Cells.Add(CrearCeldaCentro(ano.TotalGeneral.ToString(), true, Brushes.LightYellow));
                grp.Rows.Add(rValores);

                tAno.RowGroups.Add(grp);
                doc.Blocks.Add(tAno);

                // Subtabla de Poblaciones Especiales del Año
                Table tDetalle = CrearTablaMarco();
                tDetalle.Columns.Add(new TableColumn { Width = new GridLength(160) });
                tDetalle.Columns.Add(new TableColumn { Width = new GridLength(125) });
                tDetalle.Columns.Add(new TableColumn { Width = new GridLength(125) });
                tDetalle.Columns.Add(new TableColumn { Width = new GridLength(125) });
                tDetalle.Columns.Add(new TableColumn { Width = new GridLength(125) });
                tDetalle.Columns.Add(new TableColumn { Width = new GridLength(130) });

                TableRowGroup grpDet = new TableRowGroup();
                TableRow rDetHead = new TableRow { Background = Brushes.WhiteSmoke, FontSize = 8 };
                rDetHead.Cells.Add(CrearCeldaHeader("INDICADORES"));
                rDetHead.Cells.Add(CrearCeldaHeader("VENEZOLANOS"));
                rDetHead.Cells.Add(CrearCeldaHeader("EXTRANJEROS"));
                rDetHead.Cells.Add(CrearCeldaHeader("INDÍGENAS"));
                rDetHead.Cells.Add(CrearCeldaHeader("CON DISCAPACIDAD"));
                rDetHead.Cells.Add(CrearCeldaHeader("EMBARAZADAS (H)"));
                grpDet.Rows.Add(rDetHead);

                TableRow rDetVal = new TableRow { FontSize = 8.5 };
                rDetVal.Cells.Add(CrearCeldaTexto("Desglose (V / H / Total)"));
                rDetVal.Cells.Add(CrearCeldaCentro($"V: {ano.VenezolanosM} | H: {ano.VenezolanosF} ({ano.VenezolanosM + ano.VenezolanosF})"));
                rDetVal.Cells.Add(CrearCeldaCentro($"V: {ano.ExtranjerosM} | H: {ano.ExtranjerosF} ({ano.ExtranjerosM + ano.ExtranjerosF})"));
                rDetVal.Cells.Add(CrearCeldaCentro($"V: {ano.IndigenasM} | H: {ano.IndigenasF} ({ano.IndigenasM + ano.IndigenasF})"));
                rDetVal.Cells.Add(CrearCeldaCentro($"V: {ano.DiscapacidadM} | H: {ano.DiscapacidadF} ({ano.DiscapacidadM + ano.DiscapacidadF})"));
                rDetVal.Cells.Add(CrearCeldaCentro($"{ano.EmbarazadasF}"));
                grpDet.Rows.Add(rDetVal);

                tDetalle.RowGroups.Add(grpDet);
                doc.Blocks.Add(tDetalle);
                doc.Blocks.Add(new Paragraph(new Run()) { Margin = new Thickness(0, 0, 0, 4) });
            }

            // ================= 2. CONSOLIDADO GENERAL INSTITUCIONAL =================
            Table tTotalInst = CrearTablaMarco();
            tTotalInst.Columns.Add(new TableColumn { Width = new GridLength(200) });
            tTotalInst.Columns.Add(new TableColumn { Width = new GridLength(140) });
            tTotalInst.Columns.Add(new TableColumn { Width = new GridLength(140) });
            tTotalInst.Columns.Add(new TableColumn { Width = new GridLength(150) });
            tTotalInst.Columns.Add(new TableColumn { Width = new GridLength(160) });

            int granTotalM = estadisticas.Sum(x => x.TotalVarones);
            int granTotalF = estadisticas.Sum(x => x.TotalHembras);
            int granTotalGen = estadisticas.Sum(x => x.TotalGeneral);
            int granTotalInd = estadisticas.Sum(x => x.IndigenasM + x.IndigenasF);
            int granTotalDisc = estadisticas.Sum(x => x.DiscapacidadM + x.DiscapacidadF);
            int granTotalEmb = estadisticas.Sum(x => x.EmbarazadasF);

            TableRowGroup grpTot = new TableRowGroup();
            TableRow rTotTit = new TableRow { Background = Brushes.LightGray };
            rTotTit.Cells.Add(CrearCeldaHeader("MATRÍCULA TOTAL PLANTEL"));
            rTotTit.Cells.Add(CrearCeldaHeader($"VARONES (M): {granTotalM}"));
            rTotTit.Cells.Add(CrearCeldaHeader($"HEMBRAS (F): {granTotalF}"));
            rTotTit.Cells.Add(CrearCeldaHeader($"TOTAL GENERAL: {granTotalGen}"));
            rTotTit.Cells.Add(CrearCeldaHeader($"IND: {granTotalInd} | DISC: {granTotalDisc} | EMB: {granTotalEmb}"));
            grpTot.Rows.Add(rTotTit);

            tTotalInst.RowGroups.Add(grpTot);
            doc.Blocks.Add(tTotalInst);

            AgregarFirmas(doc);
            return doc;
        }

        private static TableCell CrearCeldaCentro(string texto, bool esBold = false, Brush? fondo = null)
        {
            Paragraph p = new Paragraph(new Run(texto)) { Margin = new Thickness(1), TextAlignment = TextAlignment.Center };
            if (esBold) p.FontWeight = FontWeights.Bold;
            TableCell cell = new TableCell(p)
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(0.5)
            };
            if (fondo != null) cell.Background = fondo;
            return cell;
        }

        private FlowDocument GenerarDocumentoSazeRendimiento(int gradoSeccionId, int periodoId)
        {
            List<FilaSazeRendimientoDto> lista = _reportes.ObtenerSazeRendimiento(gradoSeccionId, periodoId);
            FlowDocument doc = CrearDocumentoBase();
            AgregarMembrete(doc);

            Paragraph pTitulo = new Paragraph(new Run("FORMATO OFICIAL SAZE - RESUMEN ESTADÍSTICO DE RENDIMIENTO"))
            {
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 5, 0, 15)
            };
            doc.Blocks.Add(pTitulo);

            Table tabla = new Table { CellSpacing = 0, BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) };
            tabla.Columns.Add(new TableColumn { Width = new GridLength(170) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(130) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(60) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(65) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(65) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(65) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(65) });

            TableRowGroup grupo = new TableRowGroup();
            TableRow cab = new TableRow { Background = Brushes.LightGray };
            cab.Cells.Add(CrearCelda("Asignatura", true));
            cab.Cells.Add(CrearCelda("Docente", true));
            cab.Cells.Add(CrearCelda("Matrícula", true));
            cab.Cells.Add(CrearCelda("Evaluados", true));
            cab.Cells.Add(CrearCelda("Aprobados", true));
            cab.Cells.Add(CrearCelda("Aplazados", true));
            cab.Cells.Add(CrearCelda("% Aprob.", true));
            grupo.Rows.Add(cab);

            foreach (var r in lista)
            {
                TableRow fila = new TableRow();
                fila.Cells.Add(CrearCelda(r.Materia));
                fila.Cells.Add(CrearCelda(r.Docente));
                fila.Cells.Add(CrearCelda(r.Inscritos.ToString()));
                fila.Cells.Add(CrearCelda(r.Evaluados.ToString()));
                fila.Cells.Add(CrearCelda(r.Aprobados.ToString()));
                fila.Cells.Add(CrearCelda(r.Aplazados.ToString()));
                fila.Cells.Add(CrearCelda($"{r.PorcentajeAprobados}%", true));
                grupo.Rows.Add(fila);
            }

            tabla.RowGroups.Add(grupo);
            doc.Blocks.Add(tabla);
            doc.Blocks.Add(new Paragraph(new Run("\n")));
            AgregarFirmas(doc);
            return doc;
        }

        private static FlowDocument CrearDocumentoBase()
        {
            return new FlowDocument
            {
                PagePadding = new Thickness(45),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                PageWidth = 820
            };
        }

        private static FlowDocument DocumentoVacio(FlowDocument doc)
        {
            doc.Blocks.Add(new Paragraph(new Run("No se encontraron registros para los criterios seleccionados.")) { Foreground = Brushes.Red, FontSize = 14 });
            return doc;
        }

        private static void AgregarMembrete(FlowDocument doc)
        {
            Paragraph pMembrete = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                LineHeight = 15,
                Margin = new Thickness(0, 0, 0, 10)
            };
            pMembrete.Inlines.Add(new Bold(new Run("REPÚBLICA BOLIVARIANA DE VENEZUELA\n")));
            pMembrete.Inlines.Add(new Run("MINISTERIO DEL PODER POPULAR PARA LA EDUCACIÓN\n"));
            pMembrete.Inlines.Add(new Bold(new Run($"{EponimoLiceo}\n")));
            pMembrete.Inlines.Add(new Run($"{CodigoDea}\n"));
            pMembrete.Inlines.Add(new Run($"{UbicacionPlantel}\n"));
            doc.Blocks.Add(pMembrete);
        }

        private static void AgregarFirmas(FlowDocument doc)
        {
            Table tFirmas = new Table { Margin = new Thickness(0, 30, 0, 0) };
            tFirmas.Columns.Add(new TableColumn { Width = new GridLength(310) });
            tFirmas.Columns.Add(new TableColumn { Width = new GridLength(310) });

            TableRowGroup grp = new TableRowGroup();
            TableRow f1 = new TableRow();
            f1.Cells.Add(new TableCell(new Paragraph(new Run("_____________________________\nLcd. Director General\nSello del Plantel")) { TextAlignment = TextAlignment.Center }));
            f1.Cells.Add(new TableCell(new Paragraph(new Run("_____________________________\nDivisión de Control de Estudios\ny Evaluación")) { TextAlignment = TextAlignment.Center }));
            grp.Rows.Add(f1);

            tFirmas.RowGroups.Add(grp);
            doc.Blocks.Add(tFirmas);
        }

        private static TableCell CrearCelda(string texto, bool esBold = false)
        {
            Paragraph p = new Paragraph(new Run(texto)) { Margin = new Thickness(4) };
            if (esBold) p.FontWeight = FontWeights.Bold;
            return new TableCell(p)
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(0.5)
            };
        }

        private void btnExportarPdf_Click(object sender, RoutedEventArgs e)
        {
            if (docViewer.Document == null)
            {
                Alerta.Mostrar("Advertencia", "Genere primero la vista previa del documento antes de exportarlo a PDF.", true);
                return;
            }

            string nombreSugerido = GenerarNombreArchivoSugerido();

            SaveFileDialog sfd = new SaveFileDialog
            {
                Title = "Guardar Reporte en Formato PDF",
                Filter = "Documento PDF (*.pdf)|*.pdf",
                FileName = nombreSugerido,
                DefaultExt = ".pdf"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    ExportarDocumentoAPdf(docViewer.Document, sfd.FileName);
                    Alerta.Mostrar("Éxito", $"Documento exportado correctamente:\n{Path.GetFileName(sfd.FileName)}", false);
                }
                catch (Exception ex)
                {
                    Alerta.Mostrar("Error de Exportación", "No se pudo guardar el archivo PDF: " + ex.Message, true);
                }
            }
        }

        private static void ExportarDocumentoAPdf(FlowDocument docOriginal, string rutaDestino)
        {
            FlowDocument docClonado = ClonarFlowDocument(docOriginal);
            docClonado.PageWidth = 816;
            docClonado.PageHeight = 1056;
            docClonado.PagePadding = new Thickness(45);
            docClonado.ColumnWidth = 816;

            PrintServer printServer = new PrintServer();
            PrintQueue? pdfQueue = null;

            foreach (var q in printServer.GetPrintQueues())
            {
                if (q.Name.Contains("PDF", StringComparison.OrdinalIgnoreCase))
                {
                    pdfQueue = q;
                    break;
                }
            }

            if (pdfQueue == null)
            {
                throw new Exception("No se encontró la impresora virtual 'Microsoft Print to PDF' en este equipo. Verifique que esté activa en las características de Windows.");
            }

            XpsDocumentWriter writer = PrintQueue.CreateXpsDocumentWriter(pdfQueue);
            PrintTicket ticket = pdfQueue.DefaultPrintTicket;
            ticket.PageMediaSize = new PageMediaSize(PageMediaSizeName.NorthAmericaLetter);

            IDocumentPaginatorSource paginatorSource = docClonado;
            writer.Write(paginatorSource.DocumentPaginator, ticket);
        }

        private string GenerarNombreArchivoSugerido()
        {
            string prefijo = cmbTipoReporte.SelectedIndex switch
            {
                0 => "Constancia_Estudio",
                1 => "Constancia_Conducta",
                2 => "Notas_Certificadas",
                3 => "Boletin_Notas",
                4 => "Nomina_Seccion",
                5 => "SAZE_Matricula_Inicial",
                6 => "SAZE_Rendimiento",
                _ => "Reporte_Liceo"
            };

            string detalle = string.Empty;
            if (cmbTipoReporte.SelectedIndex < 4 && cmbEstudiantes.SelectedItem != null)
            {
                dynamic est = cmbEstudiantes.SelectedItem;
                detalle = "_" + est.Codigo;
            }
            else if (cmbGradoSeccion.SelectedItem != null)
            {
                dynamic gs = cmbGradoSeccion.SelectedItem;
                detalle = "_" + gs.Display.ToString().Replace(" ", "_").Replace("\"", "");
            }

            return $"{prefijo}{detalle}_{DateTime.Now:yyyyMMdd}.pdf";
        }

        private static FlowDocument ClonarFlowDocument(FlowDocument source)
        {
            using (var stream = new MemoryStream())
            {
                TextRange sourceRange = new TextRange(source.ContentStart, source.ContentEnd);
                sourceRange.Save(stream, DataFormats.Xaml);
                stream.Seek(0, SeekOrigin.Begin);

                FlowDocument clone = new FlowDocument();
                TextRange cloneRange = new TextRange(clone.ContentStart, clone.ContentEnd);
                cloneRange.Load(stream, DataFormats.Xaml);
                return clone;
            }
        }

        private void btnImprimir_Click(object sender, RoutedEventArgs e)
        {
            if (docViewer.Document == null)
            {
                Alerta.Mostrar("Advertencia", "Genere primero la vista previa del documento antes de imprimir.", true);
                return;
            }

            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                IDocumentPaginatorSource dps = docViewer.Document;
                printDialog.PrintDocument(dps.DocumentPaginator, "Documento Oficial Liceo");
            }
        }

    }
}