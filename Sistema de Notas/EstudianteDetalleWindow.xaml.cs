using Entidades;
using SistemaLiceo.Datos;
using System;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace SistemaLiceo.Presentacion
{
    public partial class EstudianteDetalleWindow : Window
    {
        private readonly EstudianteDatos _estudiantesDatos = new EstudianteDatos();
        private readonly RepresentanteDatos _representantesDatos = new RepresentanteDatos();
        private readonly int _estudianteId;
        private Estudiante? _estudiante;

        public EstudianteDetalleWindow(int estudianteId)
        {
            InitializeComponent();
            _estudianteId = estudianteId;
            CargarExpediente();
        }

        private void CargarExpediente()
        {
            try
            {
                _estudiante = _estudiantesDatos.ObtenerPorId(_estudianteId);
                if (_estudiante == null)
                {
                    Alerta.Mostrar("Error", "No se encontró el expediente del estudiante.", true);
                    Close();
                    return;
                }

                // Banner
                txtNombreEstudianteHeader.Text = _estudiante.Persona.NombreCompleto.ToUpper();
                txtCedulaHeader.Text = _estudiante.Persona.CedulaFormateada;
                txtCedulaEscolarHeader.Text = _estudiante.CedulaEscolar;
                txtEstadoHeader.Text = _estudiante.Estado.ToUpper();

                if (_estudiante.Estado == "Retirado")
                {
                    badgeEstado.Background = new SolidColorBrush(Color.FromRgb(254, 226, 226));
                    badgeEstado.BorderBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                    txtEstadoHeader.Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28));
                }

                // 1. Alumno
                txtFechaNac.Text = _estudiante.Persona.FechaNacimiento?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "No registrada";
                txtSexo.Text = _estudiante.Persona.Sexo == "M" ? "Masculino" : "Femenino";
                txtLateralidad.Text = _estudiante.Lateralidad;
                txtLugarNac.Text = $"{_estudiante.Persona.Nacionalidad} (ID País: {_estudiante.PaisNacimientoId})";
                txtTelAlumno.Text = string.IsNullOrWhiteSpace(_estudiante.TelefonoEstudiante) ? "No posee / No registrado" : _estudiante.TelefonoEstudiante;
                txtCorreoAlumno.Text = string.IsNullOrWhiteSpace(_estudiante.CorreoEstudiante) ? "No posee / No registrado" : _estudiante.CorreoEstudiante;

                if (_estudiante.Persona.Direccion != null)
                {
                    txtEstadoCiudadAlumno.Text = $"Ciudad ID: {_estudiante.Persona.Direccion.CiudadId}";
                    txtSectorAlumno.Text = _estudiante.Persona.Direccion.Sector ?? "S/D";
                    txtCalleAlumno.Text = $"{_estudiante.Persona.Direccion.Avenida} {_estudiante.Persona.Direccion.Calle}".Trim();
                    txtViviendaDetalleAlumno.Text = $"Nº: {_estudiante.Persona.Direccion.NumeroVivienda} - Mz: {_estudiante.Persona.Direccion.Manzana} - Ver: {_estudiante.Persona.Direccion.Vereda}";
                    txtTipoViviendaAlumno.Text = _estudiante.Persona.Direccion.TipoVivienda;
                    txtCondicionInfraAlumno.Text = $"{_estudiante.Persona.Direccion.CondicionVivienda} ({_estudiante.Persona.Direccion.InfraestructuraVivienda})";
                }
                else
                {
                    txtSectorAlumno.Text = "Sin dirección registrada";
                }

                // 2. Familia y CPNNA
                txtSituacionPadres.Text = _estudiante.SituacionPadres;
                txtConviveCon.Text = _estudiante.ConviveCon;
                txtRepresentanteTipo.Text = _estudiante.RepresentanteLegalTipo;
                txtOficioCpnna.Text = string.IsNullOrWhiteSpace(_estudiante.OficioCpnnaTribunal) ? "Ninguno (Ejerce patria potestad regular)" : _estudiante.OficioCpnnaTribunal;
                txtObsCustodia.Text = string.IsNullOrWhiteSpace(_estudiante.ObservacionesCustodia) ? "Sin observaciones especiales" : _estudiante.ObservacionesCustodia;

                // Padre
                txtPadreNombre.Text = string.IsNullOrWhiteSpace(_estudiante.PadreNombresApellidos) ? "No registrado" : _estudiante.PadreNombresApellidos;
                txtPadreCedula.Text = string.IsNullOrWhiteSpace(_estudiante.PadreCedula) ? "S/C" : _estudiante.PadreCedula;
                txtPadreTel.Text = string.IsNullOrWhiteSpace(_estudiante.PadreTelefono) ? "S/T" : _estudiante.PadreTelefono;
                txtPadreCondicion.Text = _estudiante.PadreVive;

                // Madre
                txtMadreNombre.Text = string.IsNullOrWhiteSpace(_estudiante.MadreNombresApellidos) ? "No registrada" : _estudiante.MadreNombresApellidos;
                txtMadreCedula.Text = string.IsNullOrWhiteSpace(_estudiante.MadreCedula) ? "S/C" : _estudiante.MadreCedula;
                txtMadreTel.Text = string.IsNullOrWhiteSpace(_estudiante.MadreTelefono) ? "S/T" : _estudiante.MadreTelefono;
                txtMadreCondicion.Text = _estudiante.MadreVive;

                // Representante Principal
                CargarDatosRepresentante(_estudiante.RepresentantePrincipalId);

                // 3. Salud y Canaima
                txtEstatura.Text = _estudiante.Antropometricos.Estatura.HasValue ? $"{_estudiante.Antropometricos.Estatura:N2} m" : "--";
                txtPeso.Text = _estudiante.Antropometricos.Peso.HasValue ? $"{_estudiante.Antropometricos.Peso:N2} kg" : "--";
                txtTallaCamisa.Text = _estudiante.Antropometricos.TallaCamisa ?? "--";
                txtTallaPantalon.Text = _estudiante.Antropometricos.TallaPantalon ?? "--";
                txtTallaZapato.Text = _estudiante.Antropometricos.TallaZapato?.ToString() ?? "--";

                txtAlergias.Text = _estudiante.Salud.ReaccionesAlergicas == "Si" ? $"Sí: {_estudiante.Salud.CualesAlergias}" : "No presenta";
                txtEnfermedades.Text = string.IsNullOrWhiteSpace(_estudiante.Salud.EnfermedadesPadecidas) ? "Ninguna" : _estudiante.Salud.EnfermedadesPadecidas;
                txtAtencionEspecial.Text = _estudiante.Salud.AtencionEspecial;
                txtHorarioTratamiento.Text = string.IsNullOrWhiteSpace(_estudiante.Salud.HorarioTratamiento) ? "No aplica" : _estudiante.Salud.HorarioTratamiento;
                txtEspecialista.Text = _estudiante.Salud.AtendidoPorEspecialista == "Si" ? $"Dr(a). {_estudiante.Salud.NombreEspecialista}" : "No";
                txtCondicionAtendida.Text = string.IsNullOrWhiteSpace(_estudiante.Salud.CondicionAtencion) ? "Ninguna" : _estudiante.Salud.CondicionAtencion;

                txtDeportes.Text = _estudiante.ExtraCurricular.RealizaDeportes == "Si" ? $"Sí: {_estudiante.ExtraCurricular.CualesDeportes}" : "No";
                txtPoseeCanaima.Text = _estudiante.ExtraCurricular.PoseeCanaima;
                txtSerialCanaima.Text = string.IsNullOrWhiteSpace(_estudiante.ExtraCurricular.SerialCanaima) ? "--" : _estudiante.ExtraCurricular.SerialCanaima;
                txtEstadoCanaima.Text = $"{_estudiante.ExtraCurricular.EstadoCanaima} (Cargador: {_estudiante.ExtraCurricular.EstadoCargador})";
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", "Error al cargar el expediente: " + ex.Message, true);
            }
        }

        private void CargarDatosRepresentante(int repId)
        {
            try
            {
                // Busca el representante por su ID
                DataTable dt = _representantesDatos.ListarActivos();
                foreach (DataRow r in dt.Rows)
                {
                    if (Convert.ToInt32(r["Codigo"]) == repId)
                    {
                        txtRepNombre.Text = r["Representante"].ToString();
                        txtRepCedula.Text = r["Cedula"].ToString();
                        txtRepParentesco.Text = r["Parentesco"].ToString();
                        txtRepMovil.Text = r["Telefono"].ToString();
                        txtRepCorreo.Text = r["Correo"].ToString();
                        break;
                    }
                }
            }
            catch { }
        }

        private void btnEditarFicha_Click(object sender, RoutedEventArgs e)
        {
            EstudianteForm form = new EstudianteForm(_estudianteId);
            if (form.ShowDialog() == true)
            {
                CargarExpediente(); // Refresca los datos actualizados
            }
        }

        private void btnCerrar_Click(object sender, RoutedEventArgs e) => Close();
    }
}