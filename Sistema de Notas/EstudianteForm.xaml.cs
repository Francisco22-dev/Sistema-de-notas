using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Entidades;
using SistemaLiceo.Datos;
using SistemaLiceo.Negocio;

namespace SistemaLiceo.Presentacion
{
    /// <summary>Ficha de inscripcion adaptada a la base de datos db_carabobo.</summary>
    public partial class EstudianteForm : Window
    {
        private readonly CatalogoDatos _catalogos = new CatalogoDatos();
        private readonly RepresentanteDatos _representantes = new RepresentanteDatos();
        private readonly InscripcionNegocio _negocio = new InscripcionNegocio();

        /// <summary>Id en PERSONA_REPRESENTANTE cuando la secretaria reutiliza un representante existente.</summary>
        private int _idRepresentanteSeleccionado;

        public EstudianteForm()
        {
            InitializeComponent();
            CargarCatalogos();
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

        // ===================== Combos en cascada =====================

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

        // ===================== Representante =====================

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

        // ===================== Guardar =====================

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Representante representante = ArmarRepresentante();
                Estudiante estudiante = ArmarEstudiante();
                Inscripcion inscripcion = ArmarInscripcion();

                _negocio.RegistrarInscripcionCompleta(representante, estudiante, inscripcion);

                Alerta.Mostrar("Listo", "¡Matrícula registrada con éxito!", false);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", ex.Message, true);
            }
        }

        private Representante ArmarRepresentante()
        {
            Representante representante = new Representante
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

            representante.Persona = new Persona
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

            return representante;
        }

        private Estudiante ArmarEstudiante()
        {
            Estudiante estudiante = new Estudiante
            {
                CedulaEscolar = txtCedulaEscolar.Text.Trim(),
                NumeroHijo = AEntero(txtNumeroHijo.Text) ?? 1,
                Lateralidad = TextoCombo(cmbLateralidad, "Derecha"),
                PaisNacimientoId = ValorSeleccionado(cmbPaisNacimiento),
                ParroquiaNacimientoId = ValorSeleccionadoOpcional(cmbParroquiaNacimiento)
            };

            estudiante.Persona = new Persona
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

            estudiante.Antropometricos = new Antropometricos
            {
                Estatura = ADecimal(txtEstatura.Text),
                Peso = ADecimal(txtPeso.Text),
                TallaCamisa = txtTallaCamisa.Text.Trim(),
                TallaPantalon = txtTallaPantalon.Text.Trim(),
                TallaZapato = AEntero(txtTallaZapato.Text)
            };

            estudiante.Salud = new Salud
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

            estudiante.ExtraCurricular = new ExtraCurricular
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

            return estudiante;
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

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            bool tieneDatos = !string.IsNullOrWhiteSpace(txtCedulaEscolar.Text) ||
                              !string.IsNullOrWhiteSpace(txtNombre1.Text) ||
                              !string.IsNullOrWhiteSpace(txtCedulaRep.Text);

            if (!tieneDatos)
            {
                this.Close();
                return;
            }

            MessageBoxResult resultado = MessageBox.Show(
                "¿Está seguro de que desea cancelar? Se perderán todos los datos introducidos en este formulario.",
                "Confirmar Cancelación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
                this.Close();
        }

        // ===================== Ayudantes =====================

        private static int ValorSeleccionado(ComboBox combo) =>
            combo.SelectedValue is int valor ? valor : 0;

        private static int? ValorSeleccionadoOpcional(ComboBox combo) =>
            combo.SelectedValue is int valor ? valor : (int?)null;

        private static string TextoCombo(ComboBox combo, string porDefecto)
        {
            if (combo.SelectedItem is ComboBoxItem item && item.Content != null)
                return item.Content.ToString() ?? porDefecto;
            return porDefecto;
        }

        private static decimal? ADecimal(string texto)
        {
            texto = texto.Trim().Replace(',', '.');
            if (texto.Length == 0)
                return null;
            return decimal.TryParse(texto, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal valor) ? valor : null;
        }

        private static int? AEntero(string texto)
        {
            texto = texto.Trim();
            if (texto.Length == 0)
                return null;
            return int.TryParse(texto, out int valor) ? valor : null;
        }
    }
}
