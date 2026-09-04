using Entidades;
using Microsoft.Win32;
using SistemaLiceo.Datos;
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
        private const string EponimoLiceo = "UNIDAD EDUCATIVA «LICEO BOLIVARIANO DR. ENRIQUE TEJERA»";
        private const string CodigoDea = "CÓDIGO DEA: OD05280804 | CIRCUITO EDUCATIVO Nº 4";
        private const string UbicacionPlantel = "PARROQUIA RAFAEL URDANETA, VALENCIA - ESTADO CARABOBO";

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
            ConstanciaEstudioDto? est = _reportes.ObtenerDatosConstancia(estudianteId, periodoId);
            List<FilaNotaCertificadaDto> notas = _reportes.ObtenerNotasCertificadas(estudianteId, periodoId);
            FlowDocument doc = CrearDocumentoBase();

            if (est == null) return DocumentoVacio(doc);

            string cedulaFormateada = FormatearCedula(est.Cedula, estiloCedula);

            AgregarMembrete(doc);

            Paragraph pTitulo = new Paragraph(new Run("CERTIFICACIÓN DE CALIFICACIONES\n(EDUCACIÓN MEDIA GENERAL)"))
            {
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 5, 0, 15)
            };
            doc.Blocks.Add(pTitulo);

            Paragraph pDatos = new Paragraph
            {
                FontSize = 12,
                LineHeight = 18,
                Margin = new Thickness(0, 0, 0, 12)
            };
            pDatos.Inlines.Add(new Bold(new Run("Apellidos y Nombres: ")));
            pDatos.Inlines.Add(new Run($"{est.EstudianteNombreCompleto}    "));
            pDatos.Inlines.Add(new Bold(new Run("Cédula de Identidad: ")));
            pDatos.Inlines.Add(new Run($"{cedulaFormateada}\n"));
            pDatos.Inlines.Add(new Bold(new Run("Cédula Escolar: ")));
            pDatos.Inlines.Add(new Run($"{est.CedulaEscolar}    "));
            pDatos.Inlines.Add(new Bold(new Run("Año que Cursó: ")));
            pDatos.Inlines.Add(new Run($"{est.Grado}    "));
            pDatos.Inlines.Add(new Bold(new Run("Año Escolar: ")));
            pDatos.Inlines.Add(new Run($"{est.Periodo}"));
            doc.Blocks.Add(pDatos);

            Table tabla = new Table { CellSpacing = 0, BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) };
            tabla.Columns.Add(new TableColumn { Width = new GridLength(240) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(80) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(160) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(100) });

            TableRowGroup grupo = new TableRowGroup();
            TableRow cabecera = new TableRow { Background = Brushes.LightGray };
            cabecera.Cells.Add(CrearCelda("Área de Formación / Asignatura", true));
            cabecera.Cells.Add(CrearCelda("Calificación\n(Número)", true));
            cabecera.Cells.Add(CrearCelda("Calificación\n(En Letras)", true));
            cabecera.Cells.Add(CrearCelda("Año Escolar", true));
            grupo.Rows.Add(cabecera);

            foreach (var n in notas)
            {
                TableRow fila = new TableRow();
                fila.Cells.Add(CrearCelda(n.Materia));
                fila.Cells.Add(CrearCelda(n.NotaNumero.HasValue ? n.NotaNumero.Value.ToString("D2") : "--", true));
                fila.Cells.Add(CrearCelda(n.NotaLetras));
                fila.Cells.Add(CrearCelda(n.Periodo));
                grupo.Rows.Add(fila);
            }

            tabla.RowGroups.Add(grupo);
            doc.Blocks.Add(tabla);

            Paragraph pCertificacion = new Paragraph(new Run(
                $"Quienes suscriben, Director(a) y Coordinador(a) de Control de Estudios y Evaluación de la {EponimoLiceo}, " +
                "certifican por medio de la presente que las calificaciones aquí registradas concuerdan exactamente con las actas " +
                "probatorias de evaluación que reposan en el archivo oficial de este plantel educativo."))
            {
                FontSize = 11,
                TextAlignment = TextAlignment.Justify,
                Margin = new Thickness(0, 15, 0, 10)
            };
            doc.Blocks.Add(pCertificacion);

            string fechaHoy = DateTime.Now.ToString("dd 'días del mes de' MMMM 'de' yyyy", new CultureInfo("es-ES"));
            Paragraph pFecha = new Paragraph(new Run($"Certificación que se expide en Valencia, a los {fechaHoy}."))
            {
                FontSize = 11,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 0, 0, 20)
            };
            doc.Blocks.Add(pFecha);

            AgregarFirmas(doc);
            return doc;
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

        private FlowDocument GenerarDocumentoSazeMatricula(int gradoSeccionId, int periodoId, string estiloCedula)
        {
            List<FilaSazeMatriculaDto> lista = _reportes.ObtenerSazeMatriculaInicial(gradoSeccionId, periodoId);
            FlowDocument doc = CrearDocumentoBase();
            AgregarMembrete(doc);

            Paragraph pTitulo = new Paragraph(new Run("FORMATO OFICIAL SAZE - REGISTRO DE MATRÍCULA INICIAL"))
            {
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 5, 0, 15)
            };
            doc.Blocks.Add(pTitulo);

            int totalM = lista.Count(x => x.Sexo == "M");
            int totalF = lista.Count(x => x.Sexo == "F");

            Paragraph pResumen = new Paragraph(new Run($"Total Inscritos: {lista.Count}   |   Varones (M): {totalM}   |   Hembras (F): {totalF}"))
            {
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            doc.Blocks.Add(pResumen);

            Table tabla = new Table { CellSpacing = 0, BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) };
            tabla.Columns.Add(new TableColumn { Width = new GridLength(30) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(100) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(180) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(35) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(45) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(110) });
            tabla.Columns.Add(new TableColumn { Width = new GridLength(90) });

            TableRowGroup grupo = new TableRowGroup();
            TableRow cab = new TableRow { Background = Brushes.LightGray };
            cab.Cells.Add(CrearCelda("Nº", true));
            cab.Cells.Add(CrearCelda("Cédula", true));
            cab.Cells.Add(CrearCelda("Apellidos y Nombres", true));
            cab.Cells.Add(CrearCelda("Sexo", true));
            cab.Cells.Add(CrearCelda("Edad", true));
            cab.Cells.Add(CrearCelda("Lugar Nacimiento", true));
            cab.Cells.Add(CrearCelda("Condición", true));
            grupo.Rows.Add(cab);

            foreach (var r in lista)
            {
                TableRow fila = new TableRow();
                fila.Cells.Add(CrearCelda(r.Numero.ToString()));
                fila.Cells.Add(CrearCelda(FormatearCedula(r.Cedula, estiloCedula)));
                fila.Cells.Add(CrearCelda($"{r.Apellidos} {r.Nombres}"));
                fila.Cells.Add(CrearCelda(r.Sexo));
                fila.Cells.Add(CrearCelda(r.Edad.ToString()));
                fila.Cells.Add(CrearCelda(r.LugarNacimiento));
                fila.Cells.Add(CrearCelda(r.TipoIngreso));
                grupo.Rows.Add(fila);
            }

            tabla.RowGroups.Add(grupo);
            doc.Blocks.Add(tabla);
            doc.Blocks.Add(new Paragraph(new Run("\n")));
            AgregarFirmas(doc);
            return doc;
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
            f1.Cells.Add(new TableCell(new Paragraph(new Run("_____________________________\nLcda. Directora General\nSello del Plantel")) { TextAlignment = TextAlignment.Center }));
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
            System.IO.MemoryStream stream = new System.IO.MemoryStream();
            TextRange sourceRange = new TextRange(source.ContentStart, source.ContentEnd);
            sourceRange.Save(stream, DataFormats.Xaml);

            FlowDocument clone = new FlowDocument();
            TextRange cloneRange = new TextRange(clone.ContentStart, clone.ContentEnd);
            cloneRange.Load(stream, DataFormats.Xaml);
            return clone;
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