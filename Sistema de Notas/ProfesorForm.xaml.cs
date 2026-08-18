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

        public ProfesorForm()
        {
            InitializeComponent();
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCedula.Text) ||
                string.IsNullOrWhiteSpace(txtNombre1.Text) ||
                string.IsNullOrWhiteSpace(txtApellido1.Text))
            {
                Alerta.Mostrar("Campos Vacíos", "Complete los campos obligatorios: Cédula, Primer Nombre y Primer Apellido.", true);
                return;
            }

            try
            {
                Profesor prof = new Profesor
                {
                    TipoNivel = "Secundaria",
                    Estado = "Activo",
                    Persona = new Persona
                    {
                        Nacionalidad = ((ComboBoxItem)cmbNacionalidad.SelectedItem).Content.ToString() ?? "V",
                        CedulaIdentidad = txtCedula.Text.Trim(),
                        Nombre1 = txtNombre1.Text.Trim(),
                        Nombre2 = string.IsNullOrWhiteSpace(txtNombre2.Text) ? null : txtNombre2.Text.Trim(),
                        Apellido1 = txtApellido1.Text.Trim(),
                        Apellido2 = string.IsNullOrWhiteSpace(txtApellido2.Text) ? null : txtApellido2.Text.Trim(),
                        FechaNacimiento = dpFechaNacimiento.SelectedDate,
                        Sexo = ((ComboBoxItem)cmbSexo.SelectedItem).Content.ToString() ?? "M"
                    }
                };

                _datos.Registrar(prof);
                Alerta.Mostrar("Éxito", "Profesor registrado correctamente.", false);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", ex.Message, true);
            }
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e) => Close();
    }
}