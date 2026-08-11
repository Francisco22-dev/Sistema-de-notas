using System;
using System.Windows;
using Entidades;
using SistemaLiceo.Datos;
using SistemaLiceo.Negocio;

namespace SistemaLiceo.Presentacion
{
    public partial class MainWindow : Window
    {
        private readonly UsuarioNegocio _usuarios = new UsuarioNegocio();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Avisa de una vez si la PC no alcanza al servidor de la base de datos.
            ConexionBD conexion = new ConexionBD();
            if (!conexion.ProbarRedYConexion(out string mensajeError))
                Alerta.Mostrar("Sin conexión", mensajeError, true);
        }

        private void btnIngresar_Click(object sender, RoutedEventArgs e)
        {
            string usuario = txtUsuario.Text.Trim();
            string clave = txtClave.Password.Trim();

            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(clave))
            {
                Alerta.Mostrar("Campos vacíos", "Por favor, ingrese su usuario y contraseña.", true);
                return;
            }

            try
            {
                Usuario? autenticado = _usuarios.Autenticar(usuario, clave);

                if (autenticado == null)
                {
                    Alerta.Mostrar("Acceso Denegado", "Usuario o contraseña incorrectos.", true);
                    return;
                }

                SesionActual.Iniciar(autenticado);
                Alerta.Mostrar("Acceso Concedido", $"¡Bienvenido {SesionActual.NombreUsuario}!", false);

                MenuPrincipal menu = new MenuPrincipal();
                menu.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error del Sistema", "Error de conexión: " + ex.Message, true);
            }
        }
    }
}
