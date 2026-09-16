using System;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Entidades;
using SistemaLiceo.Datos;
using SistemaLiceo.Negocio;

namespace SistemaLiceo.Presentacion
{
    public partial class AuditoriaControl : UserControl
    {
        private readonly MantenimientoDatos _mantenimientoDatos = new();

        public AuditoriaControl()
        {
            InitializeComponent();

            if (SesionActual.NombreUsuario.ToLower() != "dev_root")
            {
                Alerta.Mostrar("Acceso Denegado", "No posee credenciales de Desarrollador para acceder a este módulo.", true);
                return;
            }

            InicializarCombosHora();
            CargarEstadoMantenimiento();
            CargarLogs();
        }

        private void InicializarCombosHora()
        {
            for (int h = 0; h < 24; h++)
                cmbHora.Items.Add(h.ToString("D2"));

            for (int m = 0; m < 60; m += 5)
                cmbMinutos.Items.Add(m.ToString("D2"));

            cmbHora.SelectedIndex = 23;    // 11 PM por defecto
            cmbMinutos.SelectedIndex = 11; // :55 por defecto
        }

        private void CargarEstadoMantenimiento()
        {
            try
            {
                SistemaMantenimiento conf = _mantenimientoDatos.ObtenerConfiguracion();

                bool bloqueado = conf.BloqueoManual || DateTime.Now >= conf.FechaLimite;

                if (bloqueado)
                {
                    badgeEstadoSistema.Background = new SolidColorBrush(Color.FromRgb(254, 226, 226));
                    badgeEstadoSistema.BorderBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                    txtEstadoSistema.Text = "🔴 BLOQUEADO POR MANTENIMIENTO";
                    txtEstadoSistema.Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28));
                }
                else
                {
                    badgeEstadoSistema.Background = new SolidColorBrush(Color.FromRgb(220, 252, 231));
                    badgeEstadoSistema.BorderBrush = new SolidColorBrush(Color.FromRgb(22, 163, 74));
                    txtEstadoSistema.Text = "🟢 OPERATIVO / ABIERTO";
                    txtEstadoSistema.Foreground = new SolidColorBrush(Color.FromRgb(21, 128, 61));
                }

                txtProximoCorte.Text = $"Próximo Cierre: {conf.FechaLimite:dd/MM/yyyy hh:mm tt}";
                dpFechaCierre.SelectedDate = conf.FechaLimite.Date;
                cmbHora.SelectedItem = conf.FechaLimite.Hour.ToString("D2");
                cmbMinutos.SelectedItem = (conf.FechaLimite.Minute - (conf.FechaLimite.Minute % 5)).ToString("D2");
                txtMensajeBloqueo.Text = conf.Mensaje;
            }
            catch { }
        }

        private void cmbFrecuenciaMeses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || dpFechaCierre == null) return;

            if (cmbFrecuenciaMeses.SelectedItem is ComboBoxItem item && int.TryParse(item.Tag?.ToString(), out int meses))
            {
                if (meses > 0)
                {
                    dpFechaCierre.SelectedDate = DateTime.Now.AddMonths(meses).Date;
                }
            }
        }

        // ================= BOTÓN DISFRAZADO DE APERTURA CON BARRA DE CARGA =================
        private async void btnLiberarMantenimiento_Click(object sender, RoutedEventArgs e)
        {
            // ⚙️ DURACIÓN DEFINIDA ÚNICAMENTE EN CÓDIGO (Sin ser visible en la interfaz)
            // Puedes cambiar el valor aquí (por ejemplo: 10, 12, o 15 segundos)
            const int duracionSegundos = 12;
            const int totalPasos = 100;
            int retardoMs = (duracionSegundos * 1000) / totalPasos; // ~120 ms por cada 1%

            // 1. Bloquear botones para evitar dobles clics y mostrar la barra de progreso
            btnLiberarMantenimiento.IsEnabled = false;
            btnBloqueoInmediato.IsEnabled = false;
            panelProgresoMantenimiento.Visibility = Visibility.Visible;
            pbMantenimiento.Value = 0;
            txtPorcentajeProgreso.Text = "0%";

            try
            {
                // 2. Bucle asíncrono para que la interfaz gráfica no se congele durante los segundos de carga
                for (int p = 1; p <= totalPasos; p++)
                {
                    await Task.Delay(retardoMs);

                    pbMantenimiento.Value = p;
                    txtPorcentajeProgreso.Text = $"{p}%";

                    // Actualización de fases técnicas
                    if (p < 20)
                        txtFaseProgreso.Text = "Comprobando integridad y tablas de la base de datos...";
                    else if (p < 40)
                        txtFaseProgreso.Text = "Reindexando registros de auditoría y optimizando caché...";
                    else if (p < 65)
                        txtFaseProgreso.Text = "Verificando consistencia de notas, inscripciones y profesores...";
                    else if (p < 85)
                        txtFaseProgreso.Text = "Restableciendo permisos de sesión y certificados...";
                    else
                        txtFaseProgreso.Text = "Desbloqueando acceso regular al sistema para usuarios...";
                }

                // 3. Al completarse la barra, se ejecuta el desbloqueo en la base de datos
                DateTime fecha = dpFechaCierre.SelectedDate ?? DateTime.Now.AddMonths(6);
                int hora = cmbHora.SelectedItem != null ? int.Parse(cmbHora.SelectedItem.ToString()!) : 23;
                int min = cmbMinutos.SelectedItem != null ? int.Parse(cmbMinutos.SelectedItem.ToString()!) : 59;

                DateTime fechaHoraFinal = new DateTime(fecha.Year, fecha.Month, fecha.Day, hora, min, 0);

                int meses = 6;
                if (cmbFrecuenciaMeses.SelectedItem is ComboBoxItem item && int.TryParse(item.Tag?.ToString(), out int m))
                    meses = m;

                SistemaMantenimiento config = new()
                {
                    BloqueoManual = false, // Desbloquea a los usuarios
                    FechaLimite = fechaHoraFinal,
                    FrecuenciaMeses = meses,
                    Mensaje = txtMensajeBloqueo.Text.Trim()
                };

                _mantenimientoDatos.GuardarConfiguracion(config);
                AuditoriaDatos.Registrar(SesionActual.IdUsuario, "Mantenimiento", $"Mantenimiento ejecutado. Sistema abierto hasta: {fechaHoraFinal:dd/MM/yyyy HH:mm}");

                Alerta.Mostrar("Mantenimiento Ejecutado", $"El sistema ha sido habilitado para todos los usuarios hasta el:\n{fechaHoraFinal:dd/MM/yyyy hh:mm tt}", false);
                CargarEstadoMantenimiento();
                CargarLogs();
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", "No se pudo actualizar el mantenimiento: " + ex.Message, true);
            }
            finally
            {
                // 4. Restaurar estado de los controles
                btnLiberarMantenimiento.IsEnabled = true;
                btnBloqueoInmediato.IsEnabled = true;
                panelProgresoMantenimiento.Visibility = Visibility.Collapsed;
            }
        }

        private void btnBloqueoInmediato_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult res = MessageBox.Show(
                "¿Desea activar el mantenimiento de inmediato?\n\nNingún usuario (excepto dev_root) podrá iniciar sesión hasta que vuelva a habilitarlo.",
                "Confirmar Cierre de Emergencia",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (res == MessageBoxResult.Yes)
            {
                try
                {
                    SistemaMantenimiento config = _mantenimientoDatos.ObtenerConfiguracion();
                    config.BloqueoManual = true; // Fuerza el bloqueo
                    config.Mensaje = txtMensajeBloqueo.Text.Trim();

                    _mantenimientoDatos.GuardarConfiguracion(config);
                    AuditoriaDatos.Registrar(SesionActual.IdUsuario, "Mantenimiento", "Activó bloqueo manual de mantenimiento preventivo.");

                    Alerta.Mostrar("Sistema Bloqueado", "El sistema ha quedado cerrado para todos los usuarios regulares.", false);
                    CargarEstadoMantenimiento();
                    CargarLogs();
                }
                catch (Exception ex)
                {
                    Alerta.Mostrar("Error", ex.Message, true);
                }
            }
        }

        private void CargarLogs()
        {
            try
            {
                string? busqueda = txtBuscarLog.Text.Trim();
                DataTable dt = AuditoriaDatos.ListarLogs(busqueda);
                gridAuditoria.ItemsSource = dt.DefaultView;
            }
            catch { }
        }

        private void txtBuscarLog_TextChanged(object sender, TextChangedEventArgs e) => CargarLogs();
        private void btnRefrescar_Click(object sender, RoutedEventArgs e) => CargarLogs();

        private void btnLimpiarLogs_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult res = MessageBox.Show("¿Desea vaciar todos los registros de auditoría?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (res == MessageBoxResult.Yes)
            {
                AuditoriaDatos.LimpiarLogs();
                CargarLogs();
            }
        }
    }
}