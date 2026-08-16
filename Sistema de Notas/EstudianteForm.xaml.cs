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
                this.Title = "Editar Ficha de Estudiante";
                btnGuardar.Content = "Guardar Cambios";
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
                _estudianteActual = _negocio.ObtenerEstudiantePorId(_estudianteId);
                if (_estudianteActual == null)
                {
                    Alerta.Mostrar("Error", "No se encontró el estudiante.", true);
                    Close();
                    return;
                }

                // 1. Alumno
                cmbNacionalidad.SelectedIndex = _estudianteActual.Persona.Nacionalidad == "E" ? 1 : 0;
                txtCedula.Text = _estudianteActual.Persona.CedulaIdentidad ?? string.Empty;
                txtCedulaEscolar.Text = _estudianteActual.CedulaEscolar;
                txtNombre1.Text = _estudianteActual.Persona.Nombre1;
                txtNombre2.Text = _estudianteActual.Persona.Nombre2 ?? string.Empty;
                txtApellido1.Text = _estudianteActual.Persona.Apellido1;
                txtApellido2.Text = _estudianteActual.Persona.Apellido2 ?? string.Empty;
                dpFechaNacimiento.SelectedDate = _estudianteActual.Persona.FechaNacimiento;
                cmbSexo.SelectedIndex = _estudianteActual.Persona.Sexo == "F" ? 1 : 0;
                cmbLateralidad.Text = _estudianteActual.Lateralidad;
                txtNumeroHijo.Text = _estudianteActual.NumeroHijo.ToString();
                cmbPaisNacimiento.SelectedValue = _estudianteActual.PaisNacimientoId;

                // 2. Dirección
                if (_estudianteActual.Persona.Direccion != null)
                {
                    chkRegistrarDireccion.IsChecked = true;
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

                // 3. Salud & Antropométricos
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

                // Extra Curricular
                cmbDeportes.Text = _estudianteActual.ExtraCurricular.RealizaDeportes;
                txtCualesDeportes.Text = _estudianteActual.ExtraCurricular.CualesDeportes ?? string.Empty;
                cmbPoseeCanaima.Text = _estudianteActual.ExtraCurricular.PoseeCanaima;
                dpFechaCanaima.SelectedDate = _estudianteActual.ExtraCurricular.FechaAsignacionCanaima;
                txtSerialCanaima.Text = _estudianteActual.ExtraCurricular.SerialCanaima ?? string.Empty;
                cmbEstadoCanaima.Text = _estudianteActual.ExtraCurricular.EstadoCanaima ?? "Operativa";
                txtFallaCanaima.Text = _estudianteActual.ExtraCurricular.FallaCanaima ?? string.Empty;
                cmbPoseeCargador.Text = _estudianteActual.ExtraCurricular.PoseeCargador;
                cmbEstadoCargador.Text = _estudianteActual.ExtraCurricular.EstadoCargador ?? "Operativo";

                // 4. Representante
                _idRepresentanteSeleccionado = _estudianteActual.RepresentantePrincipalId;
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", "Error al cargar los datos del estudiante: " + ex.Message, true);
            }
        }

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
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", "No se pudieron cargar los estados: " + ex.Message, true);
            }
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
                lblRepEncontrado.Text = "Representante existente: " + encontrado.Persona.NombreCompleto;

                cmbNacionalidadRep.SelectedIndex = encontrado.Persona.Nacionalidad == "E" ? 1 : 0;
                txtNombre1Rep.Text = encontrado.Persona.Nombre1;
                txtNombre2Rep.Text = encontrado.Persona.Nombre2 ?? string.Empty;
                txtApellido1Rep.Text = encontrado.Persona.Apellido1;
                txtApellido2Rep.Text = encontrado.Persona.Apellido2 ?? string.Empty;
                dpFechaNacimientoRep.SelectedDate = encontrado.Persona.FechaNacimiento;
                cmbSexoRep.SelectedIndex = encontrado.Persona.Sexo == "M" ? 1 : 0;
                txtParentesco.Text = encontrado.Parentesco;
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

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Representante representante = ArmarRepresentante();
                Estudiante estudiante = ArmarEstudiante();
                Inscripcion inscripcion = ArmarInscripcion();

                if (_estudianteId > 0)
                {
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

                    _negocio.ActualizarInscripcionCompleta(representante, estudiante, inscripcion);
                    Alerta.Mostrar("Listo", "¡Ficha del estudiante actualizada con éxito!", false);
                }
                else
                {
                    _negocio.RegistrarInscripcionCompleta(representante, estudiante, inscripcion);
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
                Sexo = TextoCombo(cmbSexoRep, "F")
            };

            return rep;
        }

        private Estudiante ArmarEstudiante()
        {
            Estudiante est = new Estudiante
            {
                CedulaEscolar = txtCedulaEscolar.Text.Trim(),
                NumeroHijo = AEntero(txtNumeroHijo.Text) ?? 1,
                Lateralidad = TextoCombo(cmbLateralidad, "Derecha"),
                PaisNacimientoId = ValorSeleccionado(cmbPaisNacimiento),
                ParroquiaNacimientoId = ValorSeleccionadoOpcional(cmbParroquiaNacimiento)
            };

            est.Persona = new Persona
            {
                Nacionalidad = TextoCombo(cmbNacionalidad, "V"),
                CedulaIdentidad = txtCedula.Text.Trim(),
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
                throw new Exception("Seleccione el estado y la ciudad de la dirección, o desmarque el registro de dirección.");

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

        private void btnCancelar_Click(object sender, RoutedEventArgs e) => Close();

        private static int ValorSeleccionado(ComboBox combo) => combo.SelectedValue is int valor ? valor : 0;
        private static int? ValorSeleccionadoOpcional(ComboBox combo) => combo.SelectedValue is int valor ? valor : (int?)null;
        private static string TextoCombo(ComboBox combo, string porDefecto)
        {
            if (combo.SelectedItem is ComboBoxItem item && item.Content != null)
                return item.Content.ToString() ?? porDefecto;
            return porDefecto;
        }

        private static decimal? ADecimal(string texto)
        {
            texto = texto.Trim().Replace(',', '.');
            return decimal.TryParse(texto, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal valor) ? valor : null;
        }

        private static int? AEntero(string texto)
        {
            texto = texto.Trim();
            return int.TryParse(texto, out int valor) ? valor : null;
        }
    }
}