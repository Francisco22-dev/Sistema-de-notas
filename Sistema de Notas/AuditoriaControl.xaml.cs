using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using SistemaLiceo.Datos;
using SistemaLiceo.Negocio;

namespace SistemaLiceo.Presentacion
{
    public partial class AuditoriaControl : UserControl
    {
        public AuditoriaControl()
        {
            InitializeComponent();

            // Doble verificación de seguridad: solo tú puedes abrir este control
            if (SesionActual.NombreUsuario.ToLower() != "dev_root")
            {
                Alerta.Mostrar("Acceso Denegado", "No posee credenciales de Desarrollador para acceder a la auditoría.", true);
                return;
            }

            CargarLogs();
        }

        private void CargarLogs()
        {
            try
            {
                string? busqueda = txtBuscarLog.Text.Trim();
                DataTable dt = AuditoriaDatos.ListarLogs(busqueda);
                gridAuditoria.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", "Error al cargar la auditoría: " + ex.Message, true);
            }
        }

        private void txtBuscarLog_TextChanged(object sender, TextChangedEventArgs e) => CargarLogs();
        private void btnRefrescar_Click(object sender, RoutedEventArgs e) => CargarLogs();

        private void btnLimpiarLogs_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult res = MessageBox.Show(
                "¿Está seguro de que desea vaciar todo el registro de auditoría?",
                "Confirmar Acción",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (res == MessageBoxResult.Yes)
            {
                try
                {
                    AuditoriaDatos.LimpiarLogs();
                    Alerta.Mostrar("Éxito", "Historial de auditoría vaciado.", false);
                    CargarLogs();
                }
                catch (Exception ex)
                {
                    Alerta.Mostrar("Error", ex.Message, true);
                }
            }
        }
    }
}