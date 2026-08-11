using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Entidades;
using SistemaLiceo.Datos;

namespace SistemaLiceo.Presentacion
{
    /// <summary>Alta de un representante (PERSONA + PERSONA_REPRESENTANTE).</summary>
    public partial class RepresentanteForm : Window
    {
        private readonly RepresentanteDatos _datos = new RepresentanteDatos();

        public int IdRepresentanteCreado { get; private set; }
        public string CedulaCreada { get; private set; } = string.Empty;
        public string NombreCompletoCreado { get; private set; } = string.Empty;

        public RepresentanteForm() : this(string.Empty)
        {
        }

        public RepresentanteForm(string cedulaInicial)
        {
            InitializeComponent();
            txtCedula.Text = cedulaInicial;
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCedula.Text) ||
                string.IsNullOrWhiteSpace(txtNombre1.Text) ||
                string.IsNullOrWhiteSpace(txtApellido1.Text) ||
                string.IsNullOrWhiteSpace(txtParentesco.Text))
            {
                Alerta.Mostrar("Campos Vacíos",
                    "Complete los datos obligatorios: cédula, primer nombre, primer apellido y parentesco.", true);
                return;
            }

            try
            {
                Representante representante = new Representante
                {
                    Parentesco = txtParentesco.Text.Trim(),
                    EstadoCivil = TextoCombo(cmbEstadoCivil, "Soltera/o"),
                    IngresoMensual = ADecimal(txtIngresoMensual.Text),
                    TelefonoMovil = txtTelefonoMovil.Text.Trim(),
                    TelefonoHabitacion = txtTelefonoHabitacion.Text.Trim(),
                    CorreoElectronico = txtCorreo.Text.Trim(),
                    Profesion = txtProfesion.Text.Trim(),
                    EmpresaTrabajo = txtEmpresa.Text.Trim(),
                    TelefonoEmpresa = txtTelefonoEmpresa.Text.Trim(),
                    DireccionEmpresa = txtDireccionEmpresa.Text.Trim(),
                    Persona = new Persona
                    {
                        Nacionalidad = TextoCombo(cmbNacionalidad, "V"),
                        CedulaIdentidad = txtCedula.Text.Trim(),
                        Nombre1 = txtNombre1.Text.Trim(),
                        Nombre2 = txtNombre2.Text.Trim(),
                        Apellido1 = txtApellido1.Text.Trim(),
                        Apellido2 = txtApellido2.Text.Trim(),
                        FechaNacimiento = dpFechaNacimiento.SelectedDate,
                        Sexo = TextoCombo(cmbSexo, "F")
                    }
                };

                IdRepresentanteCreado = _datos.Registrar(representante);
                CedulaCreada = representante.Persona.CedulaFormateada;
                NombreCompletoCreado = representante.Persona.NombreCompleto;

                this.DialogResult = true;
                this.Close();
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

        private static decimal? ADecimal(string texto)
        {
            texto = texto.Trim().Replace(',', '.');
            if (texto.Length == 0)
                return null;
            return decimal.TryParse(texto, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal valor) ? valor : null;
        }
    }
}
