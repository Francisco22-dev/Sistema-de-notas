using System.Windows;
using System.Windows.Media;

namespace SistemaLiceo.Presentacion
{
    public partial class Alerta : Window
    {
        public Alerta(string titulo, string mensaje, bool esError)
        {
            InitializeComponent();

            txtTitulo.Text = titulo;
            txtMensaje.Text = mensaje;

            // Cambiamos los colores dependiendo del tipo de mensaje
            if (esError)
            {
                txtTitulo.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")); // Rojo moderno
                btnAceptar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
            }
            else
            {
                txtTitulo.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")); // Verde moderno
                btnAceptar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
            }
        }

        private void btnAceptar_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // Cierra la notificación al hacer clic
        }

        // 🌟 MÉTODO MAESTRO: Te permite llamar a esta ventana desde cualquier parte del programa
        public static void Mostrar(string titulo, string mensaje, bool esError = false)
        {
            Alerta alerta = new Alerta(titulo, mensaje, esError);

            // Asigna la ventana principal como dueña para que la alerta aparezca centrada sobre ella
            if (Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible)
            {
                alerta.Owner = Application.Current.MainWindow;
            }
            else
            {
                alerta.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            alerta.ShowDialog(); // Detiene el código de fondo hasta que el usuario le dé a "Aceptar"
        }
    }
}