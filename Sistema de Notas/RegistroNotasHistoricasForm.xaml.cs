using Entidades;
using SistemaLiceo.Datos;
using SistemaLiceo.Negocio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SistemaLiceo.Presentacion
{
    public partial class RegistroNotasHistoricasForm : Window
    {
        private readonly NotasCertificadasHistoricasDatos _datos = new();
        private CertificacionEstudianteCompletaDto _pensum = new();

        public RegistroNotasHistoricasForm()
        {
            InitializeComponent();
            InicializarMateriasBase();
        }

        private void InicializarMateriasBase()
        {
            _pensum.PrimerAno = CrearLista(new[] { "CASTELLANO", "INGLÉS Y OTRAS LENGUAS EXTRANJERAS", "MATEMÁTICAS", "EDUCACIÓN FÍSICA", "ARTE Y PATRIMONIO", "CIENCIAS NATURALES", "GEOGRAFÍA, HISTORIA Y CIUDADANÍA" });
            _pensum.SegundoAno = CrearLista(new[] { "CASTELLANO", "INGLÉS Y OTRAS LENGUAS EXTRANJERAS", "MATEMÁTICAS", "EDUCACIÓN FÍSICA", "ARTE Y PATRIMONIO", "CIENCIAS NATURALES", "GEOGRAFÍA, HISTORIA Y CIUDADANÍA" });
            _pensum.TercerAno = CrearLista(new[] { "CASTELLANO", "INGLÉS Y OTRAS LENGUAS EXTRANJERAS", "MATEMÁTICAS", "EDUCACIÓN FÍSICA", "FÍSICA", "QUÍMICA", "BIOLOGÍA", "GEOGRAFÍA, HISTORIA Y CIUDADANÍA" });
            _pensum.CuartoAno = CrearLista(new[] { "CASTELLANO", "INGLÉS Y OTRAS LENGUAS EXTRANJERAS", "MATEMÁTICAS", "EDUCACIÓN FÍSICA", "FÍSICA", "QUÍMICA", "BIOLOGÍA", "GEOGRAFÍA, HISTORIA Y CIUDADANÍA", "FORMACIÓN PARA LA SOBERANÍA NACIONAL" });
            _pensum.QuintoAno = CrearLista(new[] { "CASTELLANO", "INGLÉS Y OTRAS LENGUAS EXTRANJERAS", "MATEMÁTICAS", "EDUCACIÓN FÍSICA", "FÍSICA", "QUÍMICA", "BIOLOGÍA", "CIENCIAS DE LA TIERRA", "GEOGRAFÍA, HISTORIA Y CIUDADANÍA", "FORMACIÓN PARA LA SOBERANÍA NACIONAL" });

            gridAno1.ItemsSource = _pensum.PrimerAno;
            gridAno2.ItemsSource = _pensum.SegundoAno;
            gridAno3.ItemsSource = _pensum.TercerAno;
            gridAno4.ItemsSource = _pensum.CuartoAno;
            gridAno5.ItemsSource = _pensum.QuintoAno;
        }

        private static List<FilaMateriaPensumDto> CrearLista(string[] materias)
        {
            return materias.Select(m => new FilaMateriaPensumDto { Materia = m, TipoEvaluacion = "F", MesAno = "07 2026", InstitucionNro = 1 }).ToList();
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCedula.Text) || string.IsNullOrWhiteSpace(txtNombres.Text) || string.IsNullOrWhiteSpace(txtApellidos.Text))
            {
                Alerta.Mostrar("Campos Obligatorios", "Complete la cédula, nombres y apellidos del estudiante.", true);
                return;
            }

            try
            {
                // Calcular letras y promedio
                List<decimal> todasNotas = new();
                ProcesarLista(_pensum.PrimerAno, todasNotas);
                ProcesarLista(_pensum.SegundoAno, todasNotas);
                ProcesarLista(_pensum.TercerAno, todasNotas);
                ProcesarLista(_pensum.CuartoAno, todasNotas);
                ProcesarLista(_pensum.QuintoAno, todasNotas);

                decimal promedio = todasNotas.Count > 0 ? Math.Round(todasNotas.Average(), 3) : 0;
                lblPromedio.Text = $"Promedio Calculado: {promedio:N3}";

                _pensum.Cedula = txtCedula.Text.Trim();
                _pensum.Nombres = txtNombres.Text.Trim();
                _pensum.Apellidos = txtApellidos.Text.Trim();
                _pensum.FechaNacimiento = dpFechaNacimiento.SelectedDate;
                _pensum.PaisNacimiento = txtPaisNac.Text.Trim();
                _pensum.EstadoNacimiento = txtEstadoNac.Text.Trim();
                _pensum.MunicipioNacimiento = txtMunNac.Text.Trim();
                _pensum.PromedioGeneral = promedio;

                NotaCertificadaHistorica entidad = new()
                {
                    Cedula = txtCedula.Text.Trim(),
                    Nombres = txtNombres.Text.Trim(),
                    Apellidos = txtApellidos.Text.Trim(),
                    FechaNacimiento = dpFechaNacimiento.SelectedDate,
                    PaisNacimiento = txtPaisNac.Text.Trim(),
                    EstadoNacimiento = txtEstadoNac.Text.Trim(),
                    MunicipioNacimiento = txtMunNac.Text.Trim(),
                    PlantelEgreso = txtPlantel.Text.Trim(),
                    PromedioGeneral = promedio,
                    DatosPensum = _pensum
                };

                _datos.Guardar(entidad);
                AuditoriaDatos.Registrar(SesionActual.IdUsuario, "Notas Certificadas", $"Registró notas históricas de {entidad.Nombres} {entidad.Apellidos} ({entidad.Cedula})");

                Alerta.Mostrar("Éxito", "¡Notas certificadas históricas guardadas con éxito!\nYa puede buscarlas e imprimirlas desde el módulo de Reportes.", false);
                return;
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", "Error al guardar notas históricas: " + ex.Message, true);
            }
        }

        private static void ProcesarLista(List<FilaMateriaPensumDto> lista, List<decimal> acumulador)
        {
            foreach (var m in lista)
            {
                if (m.NotaNumero.HasValue)
                {
                    m.NotaLetras = ReportesDatos.ConvertirNotaEnLetras(m.NotaNumero.Value);
                    acumulador.Add(m.NotaNumero.Value);
                }
                else
                {
                    m.NotaLetras = "--";
                }
            }
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e) => Close();
    }
}