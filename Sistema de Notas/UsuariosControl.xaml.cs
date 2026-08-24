using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using SistemaLiceo.Negocio;

namespace SistemaLiceo.Presentacion
{
    public partial class UsuariosControl : UserControl
    {
        private readonly UsuarioNegocio _negocio = new UsuarioNegocio();

        public UsuariosControl()
        {
            InitializeComponent();
            CargarUsuarios();
        }

        private void CargarUsuarios()
        {
            try
            {
                DataTable dt = _negocio.ListarTodos();
                gridUsuarios.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", "No se pudieron cargar los usuarios: " + ex.Message, true);
            }
        }

        private void btnNuevoUsuario_Click(object sender, RoutedEventArgs e)
        {
            UsuarioForm form = new UsuarioForm();
            if (form.ShowDialog() == true)
                CargarUsuarios();
        }

        private void btnEditarUsuario_Click(object sender, RoutedEventArgs e)
        {
            if (gridUsuarios.SelectedItem is DataRowView fila && fila.Row.Table.Columns.Contains("Codigo"))
            {
                int usuarioId = Convert.ToInt32(fila["Codigo"]);
                UsuarioForm form = new UsuarioForm(usuarioId);
                if (form.ShowDialog() == true)
                    CargarUsuarios();
            }
            else
            {
                Alerta.Mostrar("Selección Requerida", "Seleccione un usuario de la tabla para editarlo.", true);
            }
        }

        private void btnAlternarEstado_Click(object sender, RoutedEventArgs e)
        {
            if (gridUsuarios.SelectedItem is DataRowView fila)
            {
                int usuarioId = Convert.ToInt32(fila["Codigo"]);
                string estadoActual = fila["Estado"].ToString() ?? "Activo";
                string nombre = fila["Usuario"].ToString() ?? string.Empty;

                if (usuarioId == SesionActual.IdUsuario)
                {
                    Alerta.Mostrar("Operación No Permitida", "No puede desactivar su propio usuario en sesión.", true);
                    return;
                }

                try
                {
                    _negocio.AlternarEstado(usuarioId, estadoActual);
                    Alerta.Mostrar("Éxito", $"Estado del usuario '{nombre}' actualizado.", false);
                    CargarUsuarios();
                }
                catch (Exception ex)
                {
                    Alerta.Mostrar("Error", ex.Message, true);
                }
            }
            else
            {
                Alerta.Mostrar("Selección Requerida", "Seleccione un usuario de la tabla.", true);
            }
        }
    }
}