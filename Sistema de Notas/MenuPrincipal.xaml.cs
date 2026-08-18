using System.Windows;
using SistemaLiceo.Negocio;

namespace SistemaLiceo.Presentacion
{
    public partial class MenuPrincipal : Window
    {
        public MenuPrincipal()
        {
            InitializeComponent();
            CargarDatosUsuario();
        }

        private void CargarDatosUsuario()
        {
            txtUsuarioInfo.Text = $"{SesionActual.NombreUsuario} | {SesionActual.Rol}";
        }

        private void btnEstudiantes_Click(object sender, RoutedEventArgs e)
        {
            ContenedorPrincipal.Content = new EstudiantesControl();
        }

        private void btnProfesores_Click(object sender, RoutedEventArgs e)
        {
            ContenedorPrincipal.Content = new ProfesoresControl();
        }

        private void btnNotas_Click(object sender, RoutedEventArgs e)
        {
            ContenedorPrincipal.Content = new NotasControl();
        }

        private void btnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            SesionActual.LimpiarSesion();
            MainWindow login = new MainWindow();
            login.Show();
            this.Close();
        }
    }
}