using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Entidades
{
    public class AsignacionMateriaDocenteDto : INotifyPropertyChanged
    {
        private int _profesorId;
        private string _estadoAsignacion = "Sin Asignar";

        public int MateriaId { get; set; }
        public string MateriaNombre { get; set; } = string.Empty;
        public int GradoMateriaId { get; set; }
        public int? MateriaProfesorPeriodoId { get; set; }

        public int ProfesorId
        {
            get => _profesorId;
            set
            {
                _profesorId = value;
                EstadoAsignacion = value > 0 ? "Asignado" : "Sin Asignar";
                OnPropertyChanged();
            }
        }

        public string EstadoAsignacion
        {
            get => _estadoAsignacion;
            private set
            {
                _estadoAsignacion = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ProfesorComboDto
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
    }
}