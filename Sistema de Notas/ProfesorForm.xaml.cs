using System;
using System.Windows;
using System.Windows.Controls;
using Entidades;
using SistemaLiceo.Datos;

namespace SistemaLiceo.Presentacion
{
    public partial class ProfesorForm : Window
    {
        private readonly ProfesorDatos _datos = new ProfesorDatos();
        private readonly int _profesorId;
        private Profesor? _profesorActual;

        public ProfesorForm(int profesorId = 0)
        {
            InitializeComponent();
            _profesorId = profesorId;

            if (_profesorId > 0)
            {
                Title = "Editar Ficha de Docente";
                btnGuardar.Content = "Actualizar Docente";
                CargarDatosProfesor();
            }
        }

        private void CargarDatosProfesor()
        {
            try
            {
                _profesorActual = _datos.BuscarPorId(_profesorId);
                if (_profesorActual == null)
                {
                    Alerta.Mostrar("Error", "No se encontró el profesor solicitado.", true);
                    Close();
                    return;
                }

                cmbNacionalidad.SelectedIndex = _profesorActual.Persona.Nacionalidad == "E" ? 1 : 0;
                txtCedula.Text = _profesorActual.Persona.CedulaIdentidad ?? string.Empty;
                txtNombre1.Text = _profesorActual.Persona.Nombre1;
                txtNombre2.Text = _profesorActual.Persona.Nombre2 ?? string.Empty;
                txtApellido1.Text = _profesorActual.Persona.Apellido1;
                txtApellido2.Text = _profesorActual.Persona.Apellido2 ?? string.Empty;
                dpFechaNacimiento.SelectedDate = _profesorActual.Persona.FechaNacimiento;
                cmbSexo.SelectedIndex = _profesorActual.Persona.Sexo == "F" ? 1 : 0;
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", "Error al cargar datos del profesor: " + ex.Message, true);
                Close();
            }
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            string cedula = txtCedula.Text.Trim();
            string nombre1 = txtNombre1.Text.Trim();
            string apellido1 = txtApellido1.Text.Trim();

            if (string.IsNullOrWhiteSpace(cedula) || string.IsNullOrWhiteSpace(nombre1) || string.IsNullOrWhiteSpace(apellido1))
            {
                Alerta.Mostrar("Campos Vacíos", "Complete los campos obligatorios: Cédula, Primer Nombre y Primer Apellido.", true);
                return;
            }

            try
            {
                string nacionalidad = TextoCombo(cmbNacionalidad, "V");
                string sexo = TextoCombo(cmbSexo, "M");

                Profesor prof = new Profesor
                {
                    Id = _profesorId,
                    TipoNivel = "Secundaria",
                    Estado = "Activo",
                    Persona = new Persona
                    {
                        Nacionalidad = nacionalidad,
                        CedulaIdentidad = cedula,
                        Nombre1 = nombre1,
                        Nombre2 = string.IsNullOrWhiteSpace(txtNombre2.Text) ? null : txtNombre2.Text.Trim(),
                        Apellido1 = apellido1,
                        Apellido2 = string.IsNullOrWhiteSpace(txtApellido2.Text) ? null : txtApellido2.Text.Trim(),
                        FechaNacimiento = dpFechaNacimiento.SelectedDate,
                        Sexo = sexo
                    }
                };

                if (_profesorId > 0)
                {
                    if (_profesorActual == null)
                    {
                        Alerta.Mostrar("Error", "No se puede actualizar porque no se cargó el profesor original.", true);
                        return;
                    }

                    prof.PersonaId = _profesorActual.PersonaId;
                    prof.Persona.Id = _profesorActual.Persona.Id;
                    prof.Persona.DireccionId = _profesorActual.Persona.DireccionId;

                    _datos.Actualizar(prof);
                    Alerta.Mostrar("Éxito", "Profesor actualizado correctamente.", false);
                }
                else
                {
                    _datos.Registrar(prof);
                    Alerta.Mostrar("Éxito", "Profesor registrado correctamente.", false);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", ex.Message, true);
            }
        }

        private static string TextoCombo(ComboBox combo, string porDefecto)
        {
            if (combo.SelectedItem is ComboBoxItem item && item.Content != null)
                return item.Content.ToString() ?? porDefecto;
            return porDefecto;
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e) => Close();
    }
}