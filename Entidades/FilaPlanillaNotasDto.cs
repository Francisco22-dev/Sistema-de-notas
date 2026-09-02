using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Entidades
{
    public class ColumnaEvaluacionDto
    {
        public int Numero { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public decimal Porcentaje { get; set; } = 20; // Por defecto 20% cada una
    }

    public class FilaPlanillaNotasDto : INotifyPropertyChanged
    {
        private decimal? _eval1;
        private decimal? _eval2;
        private decimal? _eval3;
        private decimal? _eval4;
        private decimal? _eval5;
        private decimal? _eval6;
        private decimal _sumatoria;
        private int _definitiva;

        public int NroLista { get; set; }
        public int InscripcionId { get; set; }
        public int EstudianteId { get; set; }
        public string Cedula { get; set; } = string.Empty;
        public string ApellidosYNombres { get; set; } = string.Empty;

        public decimal? Eval1
        {
            get => _eval1;
            set { _eval1 = LimitarNota(value); OnPropertyChanged(); Recalcular(); }
        }

        public decimal? Eval2
        {
            get => _eval2;
            set { _eval2 = LimitarNota(value); OnPropertyChanged(); Recalcular(); }
        }

        public decimal? Eval3
        {
            get => _eval3;
            set { _eval3 = LimitarNota(value); OnPropertyChanged(); Recalcular(); }
        }

        public decimal? Eval4
        {
            get => _eval4;
            set { _eval4 = LimitarNota(value); OnPropertyChanged(); Recalcular(); }
        }

        public decimal? Eval5
        {
            get => _eval5;
            set { _eval5 = LimitarNota(value); OnPropertyChanged(); Recalcular(); }
        }

        public decimal? Eval6
        {
            get => _eval6;
            set { _eval6 = LimitarNota(value); OnPropertyChanged(); Recalcular(); }
        }

        public decimal Sumatoria
        {
            get => _sumatoria;
            private set { _sumatoria = value; OnPropertyChanged(); }
        }

        public int Definitiva
        {
            get => _definitiva;
            private set { _definitiva = value; OnPropertyChanged(); }
        }

        // Ponderaciones activas para el cálculo
        public static decimal[] Ponderaciones { get; set; } = new decimal[] { 20, 20, 20, 20, 10, 10 };

        public void Recalcular()
        {
            decimal sumaPonderada = 0;
            decimal pesoTotal = 0;

            decimal?[] notas = new decimal?[] { Eval1, Eval2, Eval3, Eval4, Eval5, Eval6 };

            for (int i = 0; i < notas.Length; i++)
            {
                if (notas[i].HasValue)
                {
                    decimal peso = (i < Ponderaciones.Length && Ponderaciones[i] > 0) ? Ponderaciones[i] : 20;
                    sumaPonderada += (notas[i]!.Value * (peso / 100m));
                    pesoTotal += peso;
                }
            }

            // Si todas las ponderaciones suman 100%, la sumatoria ponderada es directa
            Sumatoria = Math.Round(sumaPonderada, 2);
            Definitiva = (int)Math.Round(Sumatoria, MidpointRounding.AwayFromZero);
        }

        private static decimal? LimitarNota(decimal? valor)
        {
            if (!valor.HasValue) return null;
            if (valor.Value < 0) return 0;
            if (valor.Value > 20) return 20;
            return Math.Round(valor.Value, 2);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}