using System;
using System.Windows;
using System.Windows.Controls;
using Entidades;
using SistemaLiceo.Negocio;

namespace SistemaLiceo.Presentacion
{
    public partial class UsuarioForm : Window
    {
        private readonly UsuarioNegocio _negocio = new UsuarioNegocio();
        private readonly int _usuarioId;
        private Usuario? _usuarioActual;

        public UsuarioForm(int usuarioId = 0)
        {
            InitializeComponent();
            _usuarioId = usuarioId;

            if (_usuarioId > 0)
            {
                lblTitulo.Text = "Editar Usuario";
                btnGuardar.Content = "Actualizar";
                lblClave.Text = "Nueva Contraseña (dejar en blanco para mantener la actual):";
                CargarDatos();
            }
        }

        private void CargarDatos()
        {
            try
            {
                _usuarioActual = _negocio.BuscarPorId(_usuarioId);
                if (_usuarioActual == null)
                {
                    Alerta.Mostrar("Error", "No se encontró el usuario.", true);
                    Close();
                    return;
                }

                txtNombre.Text = _usuarioActual.Nombre;
                cmbRol.Text = _usuarioActual.Rol;
                cmbEstado.Text = _usuarioActual.Estado;
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", ex.Message, true);
                Close();
            }
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            string nombre = txtNombre.Text.Trim();
            string clave = txtClave.Password.Trim();
            string rol = ((ComboBoxItem)cmbRol.SelectedItem).Content.ToString() ?? "Secretaria";
            string estado = ((ComboBoxItem)cmbEstado.SelectedItem).Content.ToString() ?? "Activo";

            if (string.IsNullOrWhiteSpace(nombre))
            {
                Alerta.Mostrar("Campo Requerido", "Indique el nombre de usuario.", true);
                return;
            }

            try
            {
                if (_usuarioId > 0)
                {
                    Usuario u = new Usuario
                    {
                        Id = _usuarioId,
                        Nombre = nombre,
                        Rol = rol,
                        Estado = estado
                    };

                    _negocio.Actualizar(u, string.IsNullOrWhiteSpace(clave) ? null : clave);
                    Alerta.Mostrar("Éxito", "Usuario actualizado correctamente.", false);
                }
                else
                {
                    Usuario u = new Usuario
                    {
                        Nombre = nombre,
                        Rol = rol,
                        Estado = estado
                    };

                    _negocio.Registrar(u, clave);
                    Alerta.Mostrar("Éxito", "Usuario creado correctamente.", false);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", ex.Message, true);
            }
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e) => Close();
    }
}