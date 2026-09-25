using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Project_Kitsune.Models;
using Project_Kitsune.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Project_Kitsune.ViewModels.ShellPages
{
    public partial class PlaylistDetailPageViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Music> Musicas { get; set; } = new();
        private ObservableCollection<Music> _todasAsMusicas = new();

        public ObservableCollection<Music> TodasAsMusicas
        {
            get => _todasAsMusicas;
            set { _todasAsMusicas = value; OnPropertyChanged(); }
        }

        private List<Music> _todasAsMusicasOriginal = new();

        public string NomePlaylist => _playlistAtual?.Name ?? "";

        public int NumMusicas => Musicas.Count;

        private bool _mostrarPainelAdicionar;

        public bool MostrarPainelAdicionar
        {
            get => _mostrarPainelAdicionar;
            set
            {
                _mostrarPainelAdicionar = value;
                OnPropertyChanged();
            }
        }

        private byte[]? _capa;

        public byte[]? Capa
        {
            get => _capa;
            set
            {
                _capa = value;
                OnPropertyChanged();
            }
        }

        public string DuracaoTotalFormatada
        {
            get
            {
                var totalSegundos = Musicas.Sum(m => (m.Duracao.TotalSeconds)); // supondo que existe esse campo em segundos
                var ts = TimeSpan.FromSeconds(totalSegundos);
                return ts.Hours > 0
                    ? $"{ts.Hours}h {ts.Minutes}min"
                    : $"{ts.Minutes} min";
            }
        }

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

        private int _numeroSelecionadas;

        public int NumeroSelecionadas
        {
            get => _numeroSelecionadas;
            set { _numeroSelecionadas = value; OnPropertyChanged(); }
        }

        private readonly DatabaseService _database;
        private readonly BibliotecaService _biblioteca;
        private readonly ConfiguracaoService _configuracao;
        private readonly OrdenacaoService _ordenacaoService;
        private readonly ShellViewModel _shell;
        public PlayerViewModel Player { get; }

        private Playlist _playlistAtual = null!;
        private bool EhFavoritos => _playlistAtual?.Name == "Favoritos";

        public PlaylistDetailPageViewModel(ShellViewModel shell)
        {
            _shell = shell;
            _database = App.ServiceProvider.GetRequiredService<DatabaseService>();
            _biblioteca = App.ServiceProvider.GetRequiredService<BibliotecaService>();
            _configuracao = App.ServiceProvider.GetRequiredService<ConfiguracaoService>();
            _ordenacaoService = App.ServiceProvider.GetRequiredService<OrdenacaoService>();
            Player = App.ServiceProvider.GetRequiredService<PlayerViewModel>();
            Musicas.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(DuracaoTotalFormatada));
                OnPropertyChanged(nameof(NumMusicas));
            };
        }

        public void Carregar(Playlist playlist)
        {
            _playlistAtual = playlist;
            OnPropertyChanged(nameof(NomePlaylist));

            Application.Current.Dispatcher.Invoke(() => Musicas.Clear());

            Task.Run(() =>
            {
                List<string> caminhos = EhFavoritos
                    ? _database.ListarMusicasFavoritas()
                    : _database.ListarMusicasDaPlaylist(playlist.Id);

                Application.Current.Dispatcher.Invoke(() => OnPropertyChanged(nameof(NumMusicas)));

                List<Music> musicasCarregadas = [];
                foreach (string caminho in caminhos)
                {
                    Music? musica = _biblioteca.LerMusicaPorCaminho(caminho);
                    if (musica != null)
                    {
                        musicasCarregadas.Add(musica);
                    }
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (Music m in musicasCarregadas)
                    {
                        Musicas.Add(m);
                    }
                });

                if (!EhFavoritos)
                {
                    AtualizarCapa(playlist);
                }
                else
                {
                    AtualizarCapaFavoritos();
                }
            });
        }

        public void AtualizarCapa(Playlist playlist)
        {
            string? caminhoRecente = _database.ObterCaminhoMusicaMaisRecente(playlist.Id);
            if (caminhoRecente != null)
            {
                Music? musicaRecente = _biblioteca.LerMusicaPorCaminho(caminhoRecente);
                byte[]? capaEncontrada = musicaRecente?.Image;

                Application.Current.Dispatcher.Invoke(() => Capa = capaEncontrada);
            }
        }

        public void AtualizarCapaFavoritos()
        {
            string? caminhoRecente = _database.ListarMusicasFavoritas().FirstOrDefault();
            if (caminhoRecente != null)
            {
                Music? musicaRecente = _biblioteca.LerMusicaPorCaminho(caminhoRecente);
                byte[]? capaEncontrada = musicaRecente?.Image;

                Application.Current.Dispatcher.Invoke(() => Capa = capaEncontrada);
            }
        }

        public void TocarMusica(Music musica)
        {
            Player.TocarMusica(musica, Musicas.ToList());
        }

        [RelayCommand]
        private void Voltar()
        {
            _shell.CurrentPage = new PlaylistsPageViewModel(_shell);
        }

        [RelayCommand]
        private void AbrirPainelAdicionar()
        {
            MostrarPainelAdicionar = true;
            TextoPesquisaAdicionar = string.Empty;
            NumeroSelecionadas = 0;

            var candidatas = _biblioteca.Musicas
                .Where(m => !Musicas.Any(x => x.Caminho == m.Caminho));

            _todasAsMusicasOriginal = _ordenacaoService.Ordenar(candidatas.ToList(), "Alfabetica", false);
            TodasAsMusicas = new ObservableCollection<Music>(_todasAsMusicasOriginal);
        }

        [RelayCommand]
        private void FecharPainelAdicionar()
        {
            MostrarPainelAdicionar = false;
        }

        [RelayCommand]
        private void RemoverMusica(Music musica)
        {
            if (_playlistAtual == null) return;

            if (EhFavoritos)
            {
                _database.AlternarGosto(musica.Caminho, false);
            }
            else
            {
                int musicaId = _database.ObterIdMusicaPorCaminho(musica.Caminho);
                if (musicaId == -1) return;
                _database.RemoverMusicaDaPlaylist(_playlistAtual.Id, musicaId);
            }

            var itensAtuais = Musicas.Where(m => m.Caminho != musica.Caminho).ToList();
            Musicas.Clear();
            foreach (Music m in itensAtuais) Musicas.Add(m);
        }

        public void AdicionarMusicaEscolhida(List<Music> musicas)
        {
            if (_playlistAtual == null) return;

            var itensAtuais = Musicas.ToList();

            foreach (Music music in musicas)
            {
                bool jaExiste = itensAtuais.Any(m => m.Caminho == music.Caminho);
                if (jaExiste) continue;

                if (EhFavoritos)
                {
                    _database.AlternarGosto(music.Caminho, true);
                    itensAtuais.Insert(0, music);
                    continue;
                }

                int musicaId = _database.ObterIdMusicaPorCaminho(music.Caminho);
                if (musicaId != -1)
                {
                    _database.AdicionarMusicaAPlaylist(_playlistAtual.Id, musicaId);
                    itensAtuais.Insert(0, music);
                }
            }

            Musicas.Clear();
            foreach (Music m in itensAtuais) Musicas.Add(m);

            if (EhFavoritos)
            {
                AtualizarCapaFavoritos();
            }
            else
            {
                AtualizarCapa(_playlistAtual);
            }

            MostrarPainelAdicionar = false;
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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}