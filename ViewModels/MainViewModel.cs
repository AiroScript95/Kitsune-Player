using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Project_Kitsune.Models;
using Project_Kitsune.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace Project_Kitsune.ViewModels
{
    public partial class MainViewModel : INotifyPropertyChanged
    {
        /* ESTADOS - (Campos/Propriedades) */
        public PlayerViewModel Player { get; set; }
        protected ConfiguracaoService configuracaoService;
        private readonly OrdenacaoService _ordenacaoService;
        private readonly BibliotecaService _biblioteca;
        public ObservableCollection<Music> Musicas { get; set; } = new();
        private object? _currentView;
        private const int MaxResultados = 8;
        private readonly DispatcherTimer _debounce;

        public object? CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<Music> _todasAsMusicas = new();

        public ObservableCollection<Music> TodasAsMusicas
        {
            get => _todasAsMusicas;
            set { _todasAsMusicas = value; OnPropertyChanged(); }
        }

        private List<Music> _todasAsMusicasOriginal = new();
        private string _textoPesquisaAdicionar = string.Empty;

        public string TextoPesquisaAdicionar
        {
            get => _textoPesquisaAdicionar;
            set
            {
                _textoPesquisaAdicionar = value;
                OnPropertyChanged();
                AtualizarFiltroAdicionar();
            }
        }

        private string _textoPesquisa = string.Empty;

        public string TextoPesquisa
        {
            get => _textoPesquisa;
            set
            {
                _textoPesquisa = value;
                OnPropertyChanged();
                _debounce.Stop();
                _debounce.Start(); // reinicia a contagem a cada tecla
            }
        }

        private bool _pesquisaAberta;

        public bool PesquisaAberta
        {
            get => _pesquisaAberta;
            set { _pesquisaAberta = value; OnPropertyChanged(); }
        }

        private ObservableCollection<Music> _resultados = new();

        public ObservableCollection<Music> Resultados
        {
            get => _resultados;
            private set { _resultados = value; OnPropertyChanged(); }
        }

        protected ShellViewModel _shell;
        public string Border => _shell.Border;

        public MainViewModel()
        {
            Player = App.ServiceProvider.GetRequiredService<PlayerViewModel>();
            _shell = App.ServiceProvider.GetRequiredService<ShellViewModel>();
            _ordenacaoService = App.ServiceProvider.GetRequiredService<OrdenacaoService>();
            _biblioteca = App.ServiceProvider.GetRequiredService<BibliotecaService>();

            _shell.PropertyChanged += (s, e) =>
                                {
                                    if (e.PropertyName == nameof(ShellViewModel.Border))
                                        OnPropertyChanged(nameof(Border));
                                };
            configuracaoService = App.ServiceProvider.GetRequiredService<ConfiguracaoService>(); ;
            InicializarAplicacao();
            var candidatas = _biblioteca.Musicas.Where(m => !Musicas.Any(x => x.Caminho == m.Caminho));
            _todasAsMusicasOriginal = _ordenacaoService.Ordenar(candidatas.ToList(), "Alfabetica", false);
            TodasAsMusicas = new ObservableCollection<Music>(_todasAsMusicasOriginal);
            _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _debounce.Tick += (s, e) =>
            {
                _debounce.Stop();
                Pesquisar();
            };
        }

        /* METODOS */

        private void InicializarAplicacao()
        {
            LoadingViewModel loading = new LoadingViewModel();
            loading.Terminado += () => CurrentView = App.ServiceProvider.GetRequiredService<ShellViewModel>();
            CurrentView = loading;
        }

        private void AtualizarFiltroAdicionar()
        {
            if (string.IsNullOrWhiteSpace(TextoPesquisaAdicionar))
            {
                TodasAsMusicas = new ObservableCollection<Music>(_todasAsMusicasOriginal);
                return;
            }

            var termo = TextoPesquisaAdicionar.Trim();
            var filtradas = _todasAsMusicasOriginal.Where(m =>
                (m.Titulo?.Contains(termo, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (m.Artista?.Contains(termo, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (m.Album?.Contains(termo, StringComparison.OrdinalIgnoreCase) ?? false));

            TodasAsMusicas = new ObservableCollection<Music>(filtradas);
        }

        private void Pesquisar()
        {
            string termo = TextoPesquisa.Trim();
            if (termo.Length == 0) { Resultados = new(); return; }

            var encontradas = _biblioteca.Musicas.ToList() // cópia, por causa do watcher
                .Where(m =>
                    (m.Titulo?.Contains(termo, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (m.Artista?.Contains(termo, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (m.Album?.Contains(termo, StringComparison.OrdinalIgnoreCase) ?? false))
                .Take(MaxResultados);

            Resultados = new ObservableCollection<Music>(encontradas);
        }

        [RelayCommand]
        private void TocarResultado(Music musica)
        {
            Player.TocarMusica(musica, _biblioteca.Musicas.ToList());
            TextoPesquisa = string.Empty;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}