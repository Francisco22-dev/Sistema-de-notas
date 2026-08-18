using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using SistemaLiceo.Datos;

namespace SistemaLiceo.Presentacion
{
    public partial class ProfesoresControl : UserControl
    {
        private readonly ProfesorDatos _profesorDatos = new ProfesorDatos();

        public ProfesoresControl()
        {
            InitializeComponent();
            CargarProfesores();
        }

        private void CargarProfesores()
        {
            try
            {
                DataTable dt = _profesorDatos.ListarActivos();
                gridProfesores.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", "No se pudo cargar la lista de profesores: " + ex.Message, true);
            }
        }

        private void btnNuevoProfesor_Click(object sender, RoutedEventArgs e)
        {
            ProfesorForm form = new ProfesorForm();
            if (form.ShowDialog() == true)
                CargarProfesores();
        }

        private void btnAsignarCarga_Click(object sender, RoutedEventArgs e)
        {
            AsignarCargaForm form = new AsignarCargaForm();
            form.ShowDialog();
        }
    }
}