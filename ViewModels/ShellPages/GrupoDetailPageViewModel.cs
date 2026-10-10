using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Project_Kitsune.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Project_Kitsune.ViewModels.ShellPages
{
    public partial class GrupoDetailPageViewModel : INotifyPropertyChanged
    {
        private readonly ShellViewModel _shell;
        private readonly Func<object> _voltarPara;
        public PlayerViewModel PlayerViewModel { get; }

        public ObservableCollection<Music> Musicas { get; } = new();

        private string _nome = "";

        public string Nome
        {
            get => _nome;
            private set { _nome = value; OnPropertyChanged(); }
        }

        private byte[]? _capa;

        //private Func<AlbunsPageViewModel> value;

        public byte[]? Capa
        {
            get => _capa;
            private set { _capa = value; OnPropertyChanged(); }
        }

        public int NumMusicas => Musicas.Count;

        public string DuracaoTotalFormatada
        {
            get
            {
                var ts = TimeSpan.FromSeconds(Musicas.Sum(m => m.Duracao.TotalSeconds));
                return ts.Hours > 0 ? $"{ts.Hours}h {ts.Minutes}min" : $"{ts.Minutes} min";
            }
        }

        public GrupoDetailPageViewModel(ShellViewModel shell, Func<object> voltarPara)
        {
            _shell = shell;
            PlayerViewModel = App.ServiceProvider.GetRequiredService<PlayerViewModel>();
            _voltarPara = voltarPara;

            Musicas.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(NumMusicas));
                OnPropertyChanged(nameof(DuracaoTotalFormatada));
            };
        }

        public void Carregar(GrupoMusicas grupo)
        {
            Nome = grupo.Nome;
            Capa = grupo.Capa;
            Musicas.Clear();
            foreach (Music m in grupo.Musicas) Musicas.Add(m);
        }

        public void TocarMusica(Music musica, List<Music>? lista = null)
            => PlayerViewModel.TocarMusica(musica, lista ?? Musicas.ToList());

        [RelayCommand]
        private void TocarOrdemAleatorio()
        {
            if (Musicas.Count == 0) return;

            Music[] embaralhada = Musicas.ToArray();
            Random.Shared.Shuffle(embaralhada);
            PlayerViewModel.TocarMusica(embaralhada[0], embaralhada.ToList());
        }

        [RelayCommand]
        private void Voltar() => _shell.CurrentPage = _voltarPara();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}