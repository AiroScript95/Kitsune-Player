using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Project_Kitsune.Models
{
    public class BandaEqualizador : INotifyPropertyChanged
    {
        public event Action<uint, float>? OnGanhoAlterado;

        public uint Indice { get; set; }
        public float Frequencia { get; set; } // só para mostrar label, ex: "60 Hz"

        private float _ganho;

        public float Ganho
        {
            get => _ganho;
            set
            {
                _ganho = value;
                OnPropertyChanged();
                OnGanhoAlterado?.Invoke(Indice, value);
            }
        }

        public void AtualizarGanhoSemNotificar(float valor)
        {
            _ganho = valor;
            OnPropertyChanged(nameof(Ganho));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}