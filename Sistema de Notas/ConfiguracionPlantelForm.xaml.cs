using System;
using System.Windows;
using Entidades;
using SistemaLiceo.Datos;

namespace SistemaLiceo.Presentacion
{
    public partial class ConfiguracionPlantelForm : Window
    {
        private readonly ConfiguracionPlantelDatos _datos = new ConfiguracionPlantelDatos();

        public ConfiguracionPlantelForm()
        {
            InitializeComponent();
            CargarDatos();
        }

        private void CargarDatos()
        {
            try
            {
                ConfiguracionPlantel c = _datos.Obtener();
                txtEponimo.Text = c.Eponimo;
                txtCodigoPlantel.Text = c.CodigoPlantel;
                txtCodigoPlan.Text = c.CodigoPlanEstudio;
                txtTelefono.Text = c.Telefono;
                txtDireccion.Text = c.Direccion;
                txtMunicipio.Text = c.Municipio;
                txtEntidad.Text = c.EntidadFederal;
                txtCdcee.Text = c.Cdcee;
                txtDirectorNombre.Text = c.DirectorNombre;
                txtDirectorCedula.Text = c.DirectorCedula;
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", "Error al cargar configuración: " + ex.Message, true);
            }
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtEponimo.Text) || string.IsNullOrWhiteSpace(txtCodigoPlantel.Text))
            {
                Alerta.Mostrar("Campos Obligatorios", "El epónimo y el código de plantel no pueden estar vacíos.", true);
                return;
            }

            try
            {
                ConfiguracionPlantel c = new ConfiguracionPlantel
                {
                    Eponimo = txtEponimo.Text,
                    CodigoPlantel = txtCodigoPlantel.Text,
                    CodigoPlanEstudio = txtCodigoPlan.Text,
                    Telefono = txtTelefono.Text,
                    Direccion = txtDireccion.Text,
                    Municipio = txtMunicipio.Text,
                    EntidadFederal = txtEntidad.Text,
                    Cdcee = txtCdcee.Text,
                    DirectorNombre = txtDirectorNombre.Text,
                    DirectorCedula = txtDirectorCedula.Text
                };

                _datos.Guardar(c);
                Alerta.Mostrar("Éxito", "¡Datos del plantel y epónimo actualizados correctamente!", false);
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