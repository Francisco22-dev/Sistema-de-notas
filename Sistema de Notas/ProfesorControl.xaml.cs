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

        private void btnEditarProfesor_Click(object sender, RoutedEventArgs e)
        {
            if (gridProfesores.SelectedItem is DataRowView fila && fila.Row.Table.Columns.Contains("Codigo"))
            {
                int profesorId = Convert.ToInt32(fila["Codigo"]);
                ProfesorForm form = new ProfesorForm(profesorId);
                if (form.ShowDialog() == true)
                    CargarProfesores();
            }
            else
            {
                Alerta.Mostrar("Selección Requerida", "Seleccione un profesor de la tabla para editarlo.", true);
            }
        }
    }
}