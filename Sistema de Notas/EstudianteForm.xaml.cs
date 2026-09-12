using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Entidades;
using SistemaLiceo.Datos;
using SistemaLiceo.Negocio;

namespace SistemaLiceo.Presentacion
{
    public partial class EstudianteForm : Window
    {
        private readonly CatalogoDatos _catalogos = new CatalogoDatos();
        private readonly RepresentanteDatos _representantes = new RepresentanteDatos();
        private readonly EstudianteDatos _estudiantesDatos = new EstudianteDatos();
        private readonly InscripcionNegocio _negocio = new InscripcionNegocio();

        private int _idRepresentanteSeleccionado;
        private readonly int _estudianteId;
        private Estudiante? _estudianteActual;
        private Inscripcion? _inscripcionActual;

        public EstudianteForm(int estudianteId = 0)
        {
            InitializeComponent();
            _estudianteId = estudianteId;
            CargarCatalogos();

            if (_estudianteId > 0)
            {
                Title = "Editar Ficha de Estudiante";
                btnGuardar.Content = "💾 Guardar Cambios";
                CargarDatosEstudiante();
            }
        }

        private void CargarCatalogos()
        {
            try
            {
                cmbPaisNacimiento.ItemsSource = _catalogos.ListarPaises();
                cmbPaisNacimiento.SelectedValue = Pais.VenezuelaId;

                cmbEstadoDireccion.ItemsSource = _catalogos.ListarEstados();
                cmbEstadoDireccionRep.ItemsSource = _catalogos.ListarEstados();

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

        private void CargarDatosEstudiante()
        {
            try
            {
                _estudianteActual = _estudiantesDatos.ObtenerPorId(_estudianteId);
                if (_estudianteActual == null)
                {
                    Alerta.Mostrar("Error", "No se encontró la ficha del estudiante.", true);
                    Close();
                    return;
                }

                _inscripcionActual = _estudiantesDatos.UltimaInscripcionTemp;

                // ================= 1. ALUMNO =================
                cmbNacionalidad.SelectedIndex = _estudianteActual.Persona.Nacionalidad == "E" ? 1 : 0;
                txtCedula.Text = _estudianteActual.Persona.CedulaIdentidad ?? string.Empty;
                txtCedulaEscolar.Text = _estudianteActual.CedulaEscolar;
                txtNombre1.Text = _estudianteActual.Persona.Nombre1;
                txtNombre2.Text = _estudianteActual.Persona.Nombre2 ?? string.Empty;
                txtApellido1.Text = _estudianteActual.Persona.Apellido1;
                txtApellido2.Text = _estudianteActual.Persona.Apellido2 ?? string.Empty;
                dpFechaNacimiento.SelectedDate = _estudianteActual.Persona.FechaNacimiento;
                cmbSexo.SelectedIndex = _estudianteActual.Persona.Sexo == "F" ? 1 : 0;
                txtTelefonoEstudiante.Text = _estudianteActual.TelefonoEstudiante ?? string.Empty;
                txtCorreoEstudiante.Text = _estudianteActual.CorreoEstudiante ?? string.Empty;
                cmbLateralidad.Text = _estudianteActual.Lateralidad;
                txtNumeroHijo.Text = _estudianteActual.NumeroHijo.ToString();
                cmbPaisNacimiento.SelectedValue = _estudianteActual.PaisNacimientoId;

                // Carga en cascada del lugar de nacimiento
                if (_estudianteActual.PaisNacimientoId == Pais.VenezuelaId && _estudiantesDatos.EstadoNacimientoIdTemp > 0)
                {
                    cmbEstadoNacimiento.ItemsSource = _catalogos.ListarEstados();
                    cmbEstadoNacimiento.SelectedValue = _estudiantesDatos.EstadoNacimientoIdTemp;

                    cmbMunicipioNacimiento.ItemsSource = _catalogos.ListarMunicipios(_estudiantesDatos.EstadoNacimientoIdTemp);
                    cmbMunicipioNacimiento.SelectedValue = _estudiantesDatos.MunicipioNacimientoIdTemp;

                    cmbParroquiaNacimiento.ItemsSource = _catalogos.ListarParroquias(_estudiantesDatos.MunicipioNacimientoIdTemp);
                    cmbParroquiaNacimiento.SelectedValue = _estudianteActual.ParroquiaNacimientoId;
                }

                // ================= 2. DIRECCIÓN ALUMNO =================
                if (_estudianteActual.Persona.Direccion != null)
                {
                    chkRegistrarDireccion.IsChecked = true;
                    if (_estudiantesDatos.EstadoDireccionIdTemp > 0)
                    {
                        cmbEstadoDireccion.SelectedValue = _estudiantesDatos.EstadoDireccionIdTemp;
                        cmbCiudadDireccion.ItemsSource = _catalogos.ListarCiudades(_estudiantesDatos.EstadoDireccionIdTemp);
                        cmbCiudadDireccion.SelectedValue = _estudianteActual.Persona.Direccion.CiudadId;
                    }

                    txtSector.Text = _estudianteActual.Persona.Direccion.Sector ?? string.Empty;
                    txtAvenida.Text = _estudianteActual.Persona.Direccion.Avenida ?? string.Empty;
                    txtCalle.Text = _estudianteActual.Persona.Direccion.Calle ?? string.Empty;
                    txtManzana.Text = _estudianteActual.Persona.Direccion.Manzana ?? string.Empty;
                    txtVereda.Text = _estudianteActual.Persona.Direccion.Vereda ?? string.Empty;
                    txtNumeroVivienda.Text = _estudianteActual.Persona.Direccion.NumeroVivienda ?? string.Empty;
                    cmbTipoVivienda.Text = _estudianteActual.Persona.Direccion.TipoVivienda;
                    cmbCondicionVivienda.Text = _estudianteActual.Persona.Direccion.CondicionVivienda;
                    cmbInfraestructuraVivienda.Text = _estudianteActual.Persona.Direccion.InfraestructuraVivienda;
                }
                else
                {
                    chkRegistrarDireccion.IsChecked = false;
                }

                // ================= 3. SALUD Y ANTROPOMETRÍA =================
                txtEstatura.Text = _estudianteActual.Antropometricos.Estatura?.ToString();
                txtPeso.Text = _estudianteActual.Antropometricos.Peso?.ToString();
                txtTallaCamisa.Text = _estudianteActual.Antropometricos.TallaCamisa ?? string.Empty;
                txtTallaPantalon.Text = _estudianteActual.Antropometricos.TallaPantalon ?? string.Empty;
                txtTallaZapato.Text = _estudianteActual.Antropometricos.TallaZapato?.ToString();

                cmbAlergias.Text = _estudianteActual.Salud.ReaccionesAlergicas;
                txtCualesAlergias.Text = _estudianteActual.Salud.CualesAlergias ?? string.Empty;
                txtEnfermedades.Text = _estudianteActual.Salud.EnfermedadesPadecidas ?? string.Empty;
                cmbAtencionEspecial.Text = _estudianteActual.Salud.AtencionEspecial;
                txtHorarioTratamiento.Text = _estudianteActual.Salud.HorarioTratamiento ?? string.Empty;
                cmbAtendidoEspecialista.Text = _estudianteActual.Salud.AtendidoPorEspecialista;
                txtNombreEspecialista.Text = _estudianteActual.Salud.NombreEspecialista ?? string.Empty;
                dpFechaEspecialista.SelectedDate = _estudianteActual.Salud.FechaInicioEspecialista;
                txtCondicionAtencion.Text = _estudianteActual.Salud.CondicionAtencion ?? string.Empty;

                cmbDeportes.Text = _estudianteActual.ExtraCurricular.RealizaDeportes;
                txtCualesDeportes.Text = _estudianteActual.ExtraCurricular.CualesDeportes ?? string.Empty;
                cmbPoseeCanaima.Text = _estudianteActual.ExtraCurricular.PoseeCanaima;
                dpFechaCanaima.SelectedDate = _estudianteActual.ExtraCurricular.FechaAsignacionCanaima;
                txtSerialCanaima.Text = _estudianteActual.ExtraCurricular.SerialCanaima ?? string.Empty;
                cmbEstadoCanaima.Text = _estudianteActual.ExtraCurricular.EstadoCanaima ?? "Operativa";
                txtFallaCanaima.Text = _estudianteActual.ExtraCurricular.FallaCanaima ?? string.Empty;
                cmbPoseeCargador.Text = _estudianteActual.ExtraCurricular.PoseeCargador;
                cmbEstadoCargador.Text = _estudianteActual.ExtraCurricular.EstadoCargador ?? "Operativo";

                // ================= 4. PADRES Y REPRESENTANTE =================
                cmbSituacionPadres.Text = _estudianteActual.SituacionPadres;
                cmbConviveCon.Text = _estudianteActual.ConviveCon;
                cmbRepresentanteLegalTipo.Text = _estudianteActual.RepresentanteLegalTipo;
                txtOficioCpnna.Text = _estudianteActual.OficioCpnnaTribunal ?? string.Empty;
                txtObservacionesCustodia.Text = _estudianteActual.ObservacionesCustodia ?? string.Empty;

                txtPadreCedula.Text = _estudianteActual.PadreCedula ?? string.Empty;
                txtPadreNombres.Text = _estudianteActual.PadreNombresApellidos ?? string.Empty;
                txtPadreTelefono.Text = _estudianteActual.PadreTelefono ?? string.Empty;
                cmbPadreVive.Text = _estudianteActual.PadreVive;

                txtMadreCedula.Text = _estudianteActual.MadreCedula ?? string.Empty;
                txtMadreNombres.Text = _estudianteActual.MadreNombresApellidos ?? string.Empty;
                txtMadreTelefono.Text = _estudianteActual.MadreTelefono ?? string.Empty;
                cmbMadreVive.Text = _estudianteActual.MadreVive;

                // Cargar datos completos del representante
                var rep = _estudiantesDatos.RepresentanteCargadoTemp;
                if (rep != null)
                {
                    _idRepresentanteSeleccionado = rep.Id;
                    lblRepEncontrado.Text = $"Representante: {rep.Persona.NombreCompleto}";

                    cmbNacionalidadRep.SelectedIndex = rep.Persona.Nacionalidad == "E" ? 1 : 0;
                    txtCedulaRep.Text = rep.Persona.CedulaIdentidad ?? string.Empty;
                    txtNombre1Rep.Text = rep.Persona.Nombre1;
                    txtNombre2Rep.Text = rep.Persona.Nombre2 ?? string.Empty;
                    txtApellido1Rep.Text = rep.Persona.Apellido1;
                    txtApellido2Rep.Text = rep.Persona.Apellido2 ?? string.Empty;
                    dpFechaNacimientoRep.SelectedDate = rep.Persona.FechaNacimiento;
                    cmbSexoRep.SelectedIndex = rep.Persona.Sexo == "M" ? 1 : 0;
                    txtParentesco.Text = rep.Parentesco;
                    cmbEstadoCivilRep.Text = rep.EstadoCivil;
                    txtIngresoMensual.Text = rep.IngresoMensual?.ToString();
                    txtTelefonoMovilRep.Text = rep.TelefonoMovil ?? string.Empty;
                    txtTelefonoHabRep.Text = rep.TelefonoHabitacion ?? string.Empty;
                    txtCorreoRep.Text = rep.CorreoElectronico ?? string.Empty;
                    txtProfesionRep.Text = rep.Profesion ?? string.Empty;
                    txtEmpresaRep.Text = rep.EmpresaTrabajo ?? string.Empty;
                    txtTelefonoEmpresaRep.Text = rep.TelefonoEmpresa ?? string.Empty;
                    txtDireccionEmpresaRep.Text = rep.DireccionEmpresa ?? string.Empty;

                    if (rep.Persona.Direccion != null)
                    {
                        chkMismaDireccionEstudiante.IsChecked = false;
                        panelDireccionRep.Visibility = Visibility.Visible;

                        if (_estudiantesDatos.EstadoDireccionRepIdTemp > 0)
                        {
                            cmbEstadoDireccionRep.SelectedValue = _estudiantesDatos.EstadoDireccionRepIdTemp;
                            cmbCiudadDireccionRep.ItemsSource = _catalogos.ListarCiudades(_estudiantesDatos.EstadoDireccionRepIdTemp);
                            cmbCiudadDireccionRep.SelectedValue = rep.Persona.Direccion.CiudadId;
                        }

                        txtSectorRep.Text = rep.Persona.Direccion.Sector ?? string.Empty;
                        txtAvenidaRep.Text = rep.Persona.Direccion.Avenida ?? string.Empty;
                        txtCalleRep.Text = rep.Persona.Direccion.Calle ?? string.Empty;
                        txtManzanaRep.Text = rep.Persona.Direccion.Manzana ?? string.Empty;
                        txtVeredaRep.Text = rep.Persona.Direccion.Vereda ?? string.Empty;
                        txtNumeroViviendaRep.Text = rep.Persona.Direccion.NumeroVivienda ?? string.Empty;
                        cmbTipoViviendaRep.Text = rep.Persona.Direccion.TipoVivienda;
                        cmbCondicionViviendaRep.Text = rep.Persona.Direccion.CondicionVivienda;
                        cmbInfraestructuraViviendaRep.Text = rep.Persona.Direccion.InfraestructuraVivienda;
                    }
                }

                // ================= 5. MATRÍCULA =================
                if (_inscripcionActual != null)
                {
                    cmbPeriodo.SelectedValue = _inscripcionActual.PeriodoId;
                    cmbTipoIngreso.Text = _inscripcionActual.TipoIngreso;
                    cmbNivelAcademico.Text = _inscripcionActual.NivelAcademico;
                    txtColegioProcedencia.Text = _inscripcionActual.ColegioProcedencia ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", "Error al cargar los datos del estudiante: " + ex.Message, true);
            }
        }

        // ================= Cascadas Geográficas =================

        private void cmbPaisNacimiento_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool esVenezuela = ValorSeleccionado(cmbPaisNacimiento) == Pais.VenezuelaId;
            cmbEstadoNacimiento.IsEnabled = esVenezuela;
            cmbMunicipioNacimiento.IsEnabled = esVenezuela;
            cmbParroquiaNacimiento.IsEnabled = esVenezuela;

            if (!esVenezuela)
            {
                cmbEstadoNacimiento.ItemsSource = null;
                cmbMunicipioNacimiento.ItemsSource = null;
                cmbParroquiaNacimiento.ItemsSource = null;
                return;
            }

            try
            {
                cmbEstadoNacimiento.ItemsSource = _catalogos.ListarEstados();
            }
            catch { }
        }

        private void cmbEstadoNacimiento_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int estadoId = ValorSeleccionado(cmbEstadoNacimiento);
            cmbMunicipioNacimiento.ItemsSource = estadoId == 0 ? null : _catalogos.ListarMunicipios(estadoId);
            cmbParroquiaNacimiento.ItemsSource = null;
        }

        private void cmbMunicipioNacimiento_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int municipioId = ValorSeleccionado(cmbMunicipioNacimiento);
            cmbParroquiaNacimiento.ItemsSource = municipioId == 0 ? null : _catalogos.ListarParroquias(municipioId);
        }

        private void cmbEstadoDireccion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int estadoId = ValorSeleccionado(cmbEstadoDireccion);
            cmbCiudadDireccion.ItemsSource = estadoId == 0 ? null : _catalogos.ListarCiudades(estadoId);
        }

        private void chkMismaDireccionEstudiante_Changed(object sender, RoutedEventArgs e)
        {
            if (panelDireccionRep == null) return;
            panelDireccionRep.Visibility = (chkMismaDireccionEstudiante.IsChecked == true)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void cmbEstadoDireccionRep_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int estadoId = ValorSeleccionado(cmbEstadoDireccionRep);
            cmbCiudadDireccionRep.ItemsSource = estadoId == 0 ? null : _catalogos.ListarCiudades(estadoId);
        }

        // ================= Búsqueda de Representante =================

        private void btnBuscarRep_Click(object sender, RoutedEventArgs e)
        {
            string cedula = txtCedulaRep.Text.Trim();
            if (cedula.Length == 0)
            {
                Alerta.Mostrar("Advertencia", "Escriba la cédula del representante para buscarlo.", true);
                return;
            }

            try
            {
                Representante? encontrado = _representantes.BuscarPorCedula(cedula);
                if (encontrado == null)
                {
                    _idRepresentanteSeleccionado = 0;
                    lblRepEncontrado.Text = "No registrado: se creará con los datos de esta ficha.";
                    return;
                }

                _idRepresentanteSeleccionado = encontrado.Id;
                lblRepEncontrado.Text = "Representante: " + encontrado.Persona.NombreCompleto;

                cmbNacionalidadRep.SelectedIndex = encontrado.Persona.Nacionalidad == "E" ? 1 : 0;
                txtNombre1Rep.Text = encontrado.Persona.Nombre1;
                txtNombre2Rep.Text = encontrado.Persona.Nombre2 ?? string.Empty;
                txtApellido1Rep.Text = encontrado.Persona.Apellido1;
                txtApellido2Rep.Text = encontrado.Persona.Apellido2 ?? string.Empty;
                dpFechaNacimientoRep.SelectedDate = encontrado.Persona.FechaNacimiento;
                cmbSexoRep.SelectedIndex = encontrado.Persona.Sexo == "M" ? 1 : 0;
                txtParentesco.Text = encontrado.Parentesco;
                cmbEstadoCivilRep.Text = encontrado.EstadoCivil;
                txtIngresoMensual.Text = encontrado.IngresoMensual?.ToString();
                txtTelefonoMovilRep.Text = encontrado.TelefonoMovil ?? string.Empty;
                txtTelefonoHabRep.Text = encontrado.TelefonoHabitacion ?? string.Empty;
                txtCorreoRep.Text = encontrado.CorreoElectronico ?? string.Empty;
                txtProfesionRep.Text = encontrado.Profesion ?? string.Empty;
                txtEmpresaRep.Text = encontrado.EmpresaTrabajo ?? string.Empty;
                txtTelefonoEmpresaRep.Text = encontrado.TelefonoEmpresa ?? string.Empty;
                txtDireccionEmpresaRep.Text = encontrado.DireccionEmpresa ?? string.Empty;
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", "No se pudo buscar el representante: " + ex.Message, true);
            }
        }

        // ================= Guardado / Actualización =================

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Representante representante = ArmarRepresentante();
                Estudiante estudiante = ArmarEstudiante();
                Inscripcion inscripcion = ArmarInscripcion();

                if (_estudianteId > 0)
                {
                    // Mantener todos los IDs originales para actualizar en cascada
                    estudiante.Id = _estudianteId;
                    estudiante.PersonaId = _estudianteActual!.PersonaId;
                    estudiante.Persona.Id = _estudianteActual.Persona.Id;
                    estudiante.Persona.DireccionId = _estudianteActual.Persona.DireccionId;
                    estudiante.AntropometricoId = _estudianteActual.AntropometricoId;
                    estudiante.Antropometricos.Id = _estudianteActual.AntropometricoId;
                    estudiante.SaludId = _estudianteActual.SaludId;
                    estudiante.Salud.Id = _estudianteActual.SaludId;
                    estudiante.ExtraCurricularId = _estudianteActual.ExtraCurricularId;
                    estudiante.ExtraCurricular.Id = _estudianteActual.ExtraCurricularId;

                    if (_inscripcionActual != null)
                        inscripcion.Id = _inscripcionActual.Id;

                    _estudiantesDatos.ActualizarInscripcionCompleta(representante, estudiante, inscripcion);
                    AuditoriaDatos.Registrar(SesionActual.IdUsuario, "Estudiantes", $"Actualizó ficha del estudiante {estudiante.Persona.NombreCompleto} ({estudiante.CedulaEscolar})");
                    Alerta.Mostrar("Listo", "¡Ficha del estudiante actualizada con éxito!", false);
                }
                else
                {
                    _negocio.RegistrarInscripcionCompleta(representante, estudiante, inscripcion);
                    AuditoriaDatos.Registrar(SesionActual.IdUsuario, "Inscripciones", $"Inscribió al estudiante {estudiante.Persona.NombreCompleto} ({estudiante.CedulaEscolar})");
                    Alerta.Mostrar("Listo", "¡Matrícula registrada con éxito!", false);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", ex.Message, true);
            }
        }

        private Representante ArmarRepresentante()
        {
            Representante rep = new Representante
            {
                Id = _idRepresentanteSeleccionado,
                Parentesco = txtParentesco.Text.Trim(),
                EstadoCivil = TextoCombo(cmbEstadoCivilRep, "Soltera/o"),
                IngresoMensual = ADecimal(txtIngresoMensual.Text),
                TelefonoMovil = txtTelefonoMovilRep.Text.Trim(),
                TelefonoHabitacion = txtTelefonoHabRep.Text.Trim(),
                CorreoElectronico = txtCorreoRep.Text.Trim(),
                Profesion = txtProfesionRep.Text.Trim(),
                EmpresaTrabajo = txtEmpresaRep.Text.Trim(),
                TelefonoEmpresa = txtTelefonoEmpresaRep.Text.Trim(),
                DireccionEmpresa = txtDireccionEmpresaRep.Text.Trim()
            };

            rep.Persona = new Persona
            {
                Nacionalidad = TextoCombo(cmbNacionalidadRep, "V"),
                CedulaIdentidad = txtCedulaRep.Text.Trim(),
                Nombre1 = txtNombre1Rep.Text.Trim(),
                Nombre2 = txtNombre2Rep.Text.Trim(),
                Apellido1 = txtApellido1Rep.Text.Trim(),
                Apellido2 = txtApellido2Rep.Text.Trim(),
                FechaNacimiento = dpFechaNacimientoRep.SelectedDate,
                Sexo = TextoCombo(cmbSexoRep, "F"),
                Direccion = ArmarDireccionRepresentante()
            };

            return rep;
        }

        private Estudiante ArmarEstudiante()
        {
            string cedulaIdentidad = txtCedula.Text.Trim();
            string cedulaEscolar = string.IsNullOrWhiteSpace(txtCedulaEscolar.Text) ? cedulaIdentidad : txtCedulaEscolar.Text.Trim();

            Estudiante est = new Estudiante
            {
                CedulaEscolar = cedulaEscolar,
                NumeroHijo = AEntero(txtNumeroHijo.Text) ?? 1,
                Lateralidad = TextoCombo(cmbLateralidad, "Derecha"),
                TelefonoEstudiante = txtTelefonoEstudiante.Text.Trim(),
                CorreoEstudiante = txtCorreoEstudiante.Text.Trim(),
                PaisNacimientoId = ValorSeleccionado(cmbPaisNacimiento),
                ParroquiaNacimientoId = ValorSeleccionadoOpcional(cmbParroquiaNacimiento),

                SituacionPadres = TextoCombo(cmbSituacionPadres, "Viven Juntos (Casados/Concubinato)"),
                ConviveCon = TextoCombo(cmbConviveCon, "Ambos Padres"),
                RepresentanteLegalTipo = TextoCombo(cmbRepresentanteLegalTipo, "Madre"),
                OficioCpnnaTribunal = txtOficioCpnna.Text.Trim(),
                ObservacionesCustodia = txtObservacionesCustodia.Text.Trim(),

                PadreCedula = txtPadreCedula.Text.Trim(),
                PadreNombresApellidos = txtPadreNombres.Text.Trim(),
                PadreTelefono = txtPadreTelefono.Text.Trim(),
                PadreVive = TextoCombo(cmbPadreVive, "Vivo (En el país)"),

                MadreCedula = txtMadreCedula.Text.Trim(),
                MadreNombresApellidos = txtMadreNombres.Text.Trim(),
                MadreTelefono = txtMadreTelefono.Text.Trim(),
                MadreVive = TextoCombo(cmbMadreVive, "Viva (En el país)")
            };

            est.Persona = new Persona
            {
                Nacionalidad = TextoCombo(cmbNacionalidad, "V"),
                CedulaIdentidad = cedulaIdentidad,
                Nombre1 = txtNombre1.Text.Trim(),
                Nombre2 = txtNombre2.Text.Trim(),
                Apellido1 = txtApellido1.Text.Trim(),
                Apellido2 = txtApellido2.Text.Trim(),
                FechaNacimiento = dpFechaNacimiento.SelectedDate,
                Sexo = TextoCombo(cmbSexo, "M"),
                Direccion = ArmarDireccion()
            };

            est.Antropometricos = new Antropometricos
            {
                Estatura = ADecimal(txtEstatura.Text),
                Peso = ADecimal(txtPeso.Text),
                TallaCamisa = txtTallaCamisa.Text.Trim(),
                TallaPantalon = txtTallaPantalon.Text.Trim(),
                TallaZapato = AEntero(txtTallaZapato.Text)
            };

            est.Salud = new Salud
            {
                ReaccionesAlergicas = TextoCombo(cmbAlergias, "No"),
                CualesAlergias = txtCualesAlergias.Text.Trim(),
                EnfermedadesPadecidas = txtEnfermedades.Text.Trim(),
                AtencionEspecial = TextoCombo(cmbAtencionEspecial, "No"),
                HorarioTratamiento = txtHorarioTratamiento.Text.Trim(),
                AtendidoPorEspecialista = TextoCombo(cmbAtendidoEspecialista, "No"),
                NombreEspecialista = txtNombreEspecialista.Text.Trim(),
                FechaInicioEspecialista = dpFechaEspecialista.SelectedDate,
                CondicionAtencion = txtCondicionAtencion.Text.Trim()
            };

            est.ExtraCurricular = new ExtraCurricular
            {
                RealizaDeportes = TextoCombo(cmbDeportes, "No"),
                CualesDeportes = txtCualesDeportes.Text.Trim(),
                PoseeCanaima = TextoCombo(cmbPoseeCanaima, "No"),
                FechaAsignacionCanaima = dpFechaCanaima.SelectedDate,
                SerialCanaima = txtSerialCanaima.Text.Trim(),
                EstadoCanaima = TextoCombo(cmbEstadoCanaima, "Operativa"),
                FallaCanaima = txtFallaCanaima.Text.Trim(),
                PoseeCargador = TextoCombo(cmbPoseeCargador, "No"),
                EstadoCargador = TextoCombo(cmbEstadoCargador, "Operativo")
            };

            return est;
        }

        private Direccion? ArmarDireccion()
        {
            if (chkRegistrarDireccion.IsChecked != true)
                return null;

            int ciudadId = ValorSeleccionado(cmbCiudadDireccion);
            if (ciudadId == 0)
                throw new Exception("Seleccione el estado y la ciudad de la dirección del estudiante, o desmarque el registro de dirección.");

            return new Direccion
            {
                CiudadId = ciudadId,
                Sector = txtSector.Text.Trim(),
                Avenida = txtAvenida.Text.Trim(),
                Calle = txtCalle.Text.Trim(),
                Manzana = txtManzana.Text.Trim(),
                Vereda = txtVereda.Text.Trim(),
                NumeroVivienda = txtNumeroVivienda.Text.Trim(),
                TipoVivienda = TextoCombo(cmbTipoVivienda, "Casa"),
                CondicionVivienda = TextoCombo(cmbCondicionVivienda, "Propia"),
                InfraestructuraVivienda = TextoCombo(cmbInfraestructuraVivienda, "Buena")
            };
        }

        private Direccion? ArmarDireccionRepresentante()
        {
            if (chkMismaDireccionEstudiante.IsChecked == true)
                return ArmarDireccion();

            int ciudadId = ValorSeleccionado(cmbCiudadDireccionRep);
            if (ciudadId == 0)
                throw new Exception("Seleccione el estado y la ciudad de la dirección del representante legal, o marque la opción de heredar la dirección del estudiante.");

            return new Direccion
            {
                CiudadId = ciudadId,
                Sector = txtSectorRep.Text.Trim(),
                Avenida = txtAvenidaRep.Text.Trim(),
                Calle = txtCalleRep.Text.Trim(),
                Manzana = txtManzanaRep.Text.Trim(),
                Vereda = txtVeredaRep.Text.Trim(),
                NumeroVivienda = txtNumeroViviendaRep.Text.Trim(),
                TipoVivienda = TextoCombo(cmbTipoViviendaRep, "Casa"),
                CondicionVivienda = TextoCombo(cmbCondicionViviendaRep, "Propia"),
                InfraestructuraVivienda = TextoCombo(cmbInfraestructuraViviendaRep, "Buena")
            };
        }

        private Inscripcion ArmarInscripcion()
        {
            int gradoId = ValorSeleccionado(cmbGrado);
            int seccionId = ValorSeleccionado(cmbSeccion);

            if (gradoId == 0 || seccionId == 0)
                throw new Exception("Seleccione el grado y la sección de la matrícula.");

            return new Inscripcion
            {
                PeriodoId = ValorSeleccionado(cmbPeriodo),
                GradoSeccionId = _catalogos.ObtenerOCrearGradoSeccion(gradoId, seccionId),
                TipoIngreso = TextoCombo(cmbTipoIngreso, "Nuevo Ingreso"),
                NivelAcademico = TextoCombo(cmbNivelAcademico, "Media General"),
                ColegioProcedencia = txtColegioProcedencia.Text.Trim(),
                FechaInscripcion = DateTime.Now
            };
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            bool tieneDatos = !string.IsNullOrWhiteSpace(txtCedula.Text) ||
                              !string.IsNullOrWhiteSpace(txtNombre1.Text) ||
                              !string.IsNullOrWhiteSpace(txtCedulaRep.Text);

            if (!tieneDatos)
            {
                Close();
                return;
            }

            MessageBoxResult resultado = MessageBox.Show(
                "¿Está seguro de que desea cancelar? Se perderán todos los cambios introducidos.",
                "Confirmar Cancelación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
                Close();
        }

        private static int ValorSeleccionado(ComboBox combo) =>
            combo.SelectedValue is int valor ? valor : 0;

        private static int? ValorSeleccionadoOpcional(ComboBox combo) =>
            combo.SelectedValue is int valor ? valor : (int?)null;

        private static string TextoCombo(ComboBox combo, string porDefecto)
        {
            if (combo.SelectedItem is ComboBoxItem item && item.Content != null)
                return item.Content.ToString() ?? porDefecto;
            if (!string.IsNullOrWhiteSpace(combo.Text))
                return combo.Text;
            return porDefecto;
        }

        private static decimal? ADecimal(string texto)
        {
            texto = texto.Trim().Replace(',', '.');
            if (texto.Length == 0) return null;
            return decimal.TryParse(texto, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal valor) ? valor : null;
        }

        private static int? AEntero(string texto)
        {
            texto = texto.Trim();
            if (texto.Length == 0) return null;
            return int.TryParse(texto, out int valor) ? valor : null;
        }
    }
}