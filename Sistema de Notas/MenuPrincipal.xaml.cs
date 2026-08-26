using System.Windows;
using SistemaLiceo.Negocio;

namespace SistemaLiceo.Presentacion
{
    public partial class MenuPrincipal : Window
    {
        public MenuPrincipal()
        {
            InitializeComponent();
            AplicarPermisosPorRol();
        }

        private void AplicarPermisosPorRol()
        {
            txtUsuarioInfo.Text = $"{SesionActual.NombreUsuario} | Rol: {SesionActual.Rol}";

            // 1. RESTRICCIONES PARA SECRETARIA
            if (SesionActual.EsSecretaria)
            {
                // La secretaria no tiene acceso a crear o cambiar usuarios del sistema
                btnUsuarios.Visibility = Visibility.Collapsed;
            }
            // 2. RESTRICCIONES PARA DOCENTE
            else if (SesionActual.EsDocente)
            {
                btnEstudiantes.Visibility = Visibility.Collapsed;
                btnProfesores.Visibility = Visibility.Collapsed;
                btnMaterias.Visibility = Visibility.Collapsed;
                btnUsuarios.Visibility = Visibility.Collapsed;

                // Muestra directo la carga de notas al docente
                ContenedorPrincipal.Content = new NotasControl();
            }
            // 3. ADMINISTRADOR: Tiene acceso visible a todos los módulos
            else
            {
                btnUsuarios.Visibility = Visibility.Visible;
                btnMaterias.Visibility = Visibility.Visible;
                btnProfesores.Visibility = Visibility.Visible;
                btnEstudiantes.Visibility = Visibility.Visible;
                btnNotas.Visibility = Visibility.Visible;
                btnReportes.Visibility = Visibility.Visible;
            }
        }

        private void btnEstudiantes_Click(object sender, RoutedEventArgs e) => ContenedorPrincipal.Content = new EstudiantesControl();
        private void btnProfesores_Click(object sender, RoutedEventArgs e) => ContenedorPrincipal.Content = new ProfesoresControl();
        private void btnMaterias_Click(object sender, RoutedEventArgs e) => ContenedorPrincipal.Content = new MateriasControl();
        private void btnNotas_Click(object sender, RoutedEventArgs e) => ContenedorPrincipal.Content = new NotasControl();
        private void btnReportes_Click(object sender, RoutedEventArgs e) => ContenedorPrincipal.Content = new ReportesControl();
        private void btnUsuarios_Click(object sender, RoutedEventArgs e)
        {
            if (!SesionActual.EsAdministrador)
            {
                Alerta.Mostrar("Acceso Restringido", "Solo los Administradores tienen autorización para gestionar usuarios.", true);
                return;
            }
            ContenedorPrincipal.Content = new UsuariosControl();
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