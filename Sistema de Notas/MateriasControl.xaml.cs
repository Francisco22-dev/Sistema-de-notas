using Entidades;
using SistemaLiceo.Datos;
using SistemaLiceo.Negocio;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SistemaLiceo.Presentacion
{
    public partial class MateriasControl : UserControl
    {
        private readonly CatalogoDatos _catalogos = new CatalogoDatos();
        private int _idMateriaSeleccionada = 0;

        public MateriasControl()
        {
            InitializeComponent();
            CargarMaterias();
        }

        private void CargarMaterias()
        {
            try
            {
                List<Catalogo> materias = _catalogos.ListarMaterias();
                gridMaterias.ItemsSource = null;
                gridMaterias.ItemsSource = materias;
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", "No se pudieron cargar las materias: " + ex.Message, true);
            }
        }

        private void gridMaterias_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridMaterias.SelectedItem is Catalogo mat)
            {
                _idMateriaSeleccionada = mat.Id;
                txtNombreMateria.Text = mat.Nombre;
                btnGuardarMateria.Content = "✏️ Actualizar";
                btnCancelarEdicion.Visibility = Visibility.Visible;
            }
        }

        private void btnGuardarMateria_Click(object sender, RoutedEventArgs e)
        {
            string nombre = txtNombreMateria.Text.Trim();
            if (string.IsNullOrWhiteSpace(nombre))
            {
                Alerta.Mostrar("Campo Obligatorio", "Escriba el nombre de la materia.", true);
                return;
            }

            try
            {
                if (_catalogos.ExisteMateria(nombre, _idMateriaSeleccionada))
                {
                    Alerta.Mostrar("Duplicado", "Ya existe una materia registrada con este nombre.", true);
                    return;
                }

                if (_idMateriaSeleccionada > 0)
                {
                    _catalogos.ActualizarMateria(_idMateriaSeleccionada, nombre);
                    Alerta.Mostrar("Éxito", "Materia actualizada correctamente.", false);
                }
                else
                {
                    _catalogos.RegistrarMateria(nombre);
                    Alerta.Mostrar("Éxito", "Materia registrada correctamente.", false);
                }

                LimpiarFormulario();
                CargarMaterias();
            }
            catch (Exception ex)
            {
                Alerta.Mostrar("Error", ex.Message, true);
            }
        }

        private void btnEliminarMateria_Click(object sender, RoutedEventArgs e)
        {
            if (!SesionActual.EsAdministrador)
            {
                Alerta.Mostrar("Permiso Denegado", "Solo un usuario con rol de Administrador puede eliminar materias del pensum general.", true);
                return;
            }

            if (_idMateriaSeleccionada == 0)
            {
                Alerta.Mostrar("Selección Requerida", "Seleccione una materia de la tabla para eliminarla.", true);
                return;
            }

            MessageBoxResult res = MessageBox.Show(
                $"¿Está seguro de que desea eliminar la materia '{txtNombreMateria.Text}'?",
                "Confirmar Eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (res == MessageBoxResult.Yes)
            {
                try
                {
                    _catalogos.EliminarMateria(_idMateriaSeleccionada);
                    Alerta.Mostrar("Éxito", "Materia eliminada del sistema.", false);
                    LimpiarFormulario();
                    CargarMaterias();
                }
                catch (Exception ex)
                {
                    Alerta.Mostrar("Error", ex.Message, true);
                }
            }
        }

        private void btnCancelarEdicion_Click(object sender, RoutedEventArgs e) => LimpiarFormulario();

        private void LimpiarFormulario()
        {
            _idMateriaSeleccionada = 0;
            txtNombreMateria.Clear();
            btnGuardarMateria.Content = "💾 Guardar";
            btnCancelarEdicion.Visibility = Visibility.Collapsed;
            gridMaterias.SelectedItem = null;
        }
    }
}