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

            // DOCENTE: Acceso único y exclusivo a Notas
            if (SesionActual.EsDocente)
            {
                btnEstudiantes.Visibility = Visibility.Collapsed;
                btnProfesores.Visibility = Visibility.Collapsed;
                btnMaterias.Visibility = Visibility.Collapsed;
                btnReportes.Visibility = Visibility.Collapsed;
                btnUsuarios.Visibility = Visibility.Collapsed;
                btnAuditoria.Visibility = Visibility.Collapsed;
                btnNotas.Visibility = Visibility.Visible;

                ContenedorPrincipal.Content = new NotasControl();
            }
            // SECRETARIA: Todo operativo excepto usuarios y auditoría secreta
            else if (SesionActual.EsSecretaria)
            {
                btnEstudiantes.Visibility = Visibility.Visible;
                btnProfesores.Visibility = Visibility.Visible;
                btnMaterias.Visibility = Visibility.Visible;
                btnNotas.Visibility = Visibility.Visible;
                btnReportes.Visibility = Visibility.Visible;
                btnUsuarios.Visibility = Visibility.Collapsed;
                btnAuditoria.Visibility = Visibility.Collapsed;
            }
            // ADMINISTRADOR
            else
            {
                btnEstudiantes.Visibility = Visibility.Visible;
                btnProfesores.Visibility = Visibility.Visible;
                btnMaterias.Visibility = Visibility.Visible;
                btnNotas.Visibility = Visibility.Visible;
                btnReportes.Visibility = Visibility.Visible;
                btnUsuarios.Visibility = Visibility.Visible;

                // BOTÓN SECRETO: Solo visible para tu usuario desarrollador ('dev_root')
                btnAuditoria.Visibility = (SesionActual.NombreUsuario.ToLower() == "dev_root")
                    ? Visibility.Visible
                    : Visibility.Collapsed;
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
        private void btnAuditoria_Click(object sender, RoutedEventArgs e)
        {
            if (SesionActual.NombreUsuario.ToLower() != "dev_root")
            {
                Alerta.Mostrar("Acceso Restringido", "Este apartado solo es accesible por el Desarrollador del sistema.", true);
                return;
            }

            ContenedorPrincipal.Content = new AuditoriaControl();
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