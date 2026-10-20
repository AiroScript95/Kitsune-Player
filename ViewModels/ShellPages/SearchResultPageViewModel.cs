using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Project_Kitsune.Models;
using Project_Kitsune.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Project_Kitsune.ViewModels.ShellPages.SearchPages;

namespace Project_Kitsune.ViewModels.ShellPages
{
    public partial class SearchResultPageViewModel : INotifyPropertyChanged
    {
        private readonly ShellViewModel _shell;
        public string searchString { get; }
        private readonly BibliotecaService _biblioteca;
        public PlayerViewModel PlayerViewModel { get; }
        private const int maxResultado = 6;
        public bool verTodosMusicas { get; set; } = false;
        public bool verTodosArtistas { get; set; } = false;
        public bool verTodosAlbuns { get; set; } = false;
        public bool verTodosPlaylist { get; set; } = false;

        private ObservableCollection<Music> _resultadosMusica = new();

        public ObservableCollection<Music> ResultadosMusica {
            get => _resultadosMusica;
            private set { _resultadosMusica = value; OnPropertyChanged(); }
        }

        private ObservableCollection<GrupoMusicas> _resultadosAlbuns = new();

        public ObservableCollection<GrupoMusicas> ResultadosAlbuns {
            get => _resultadosAlbuns;
            private set { _resultadosAlbuns = value; OnPropertyChanged(); }
        }

        private ObservableCollection<GrupoMusicas> _resultadosArtistas = new();

        public ObservableCollection<GrupoMusicas> ResultadosArtistas {
            get => _resultadosArtistas;
            private set { _resultadosArtistas = value; OnPropertyChanged(); }
        }

        // Propriedade nova
        private ObservableCollection<PlaylistComCapa> _resultadosPlaylists = new();

        public ObservableCollection<PlaylistComCapa> ResultadosPlaylists {
            get => _resultadosPlaylists;
            private set { _resultadosPlaylists = value; OnPropertyChanged(); }
        }

        public SearchResultPageViewModel(ShellViewModel shell, string resultado)
        {
            _shell = shell;
            searchString = resultado;
            _biblioteca = App.ServiceProvider.GetRequiredService<BibliotecaService>();
            PlayerViewModel = App.ServiceProvider.GetRequiredService<PlayerViewModel>();
            _ = Pesquisar();
        }

        private async Task Pesquisar()
        {
            string termo = searchString.Trim();
            if (termo.Length == 0) {
                ResultadosMusica = new();
                ResultadosAlbuns = new();
                ResultadosArtistas = new();
                ResultadosPlaylists = new();
                return;
            }

            var grupos = App.ServiceProvider.GetRequiredService<GruposService>();
            var albuns = await grupos.ObterAlbunsAsync();
            var artistas = await grupos.ObterArtistasAsync();

            var musicasEncontradas = _biblioteca.Musicas.ToList().Where(m =>
                ( m.Titulo?.Contains(termo, StringComparison.OrdinalIgnoreCase) ?? false ) ||
                ( m.Artista?.Contains(termo, StringComparison.OrdinalIgnoreCase) ?? false ) ||
                ( m.Album?.Contains(termo, StringComparison.OrdinalIgnoreCase) ?? false )).ToList();

            var albunsEncontrados = albuns.Where(g => g.Nome?.Contains(termo, StringComparison.OrdinalIgnoreCase) ?? false).ToList();

            var artistasEncontrados = artistas.Where(g => g.Nome?.Contains(termo, StringComparison.OrdinalIgnoreCase) ?? false).ToList();

            var playlistsEncontradas = new PlaylistsPageViewModel(_shell).Playlists.Where(p =>
                p.Playlist.Id != Playlist.IdComLetra &&
                ( p.Playlist.Name?.Contains(termo, StringComparison.OrdinalIgnoreCase) ?? false )).ToList();

            verTodosMusicas = musicasEncontradas.Count > maxResultado;
            verTodosAlbuns = albunsEncontrados.Count > maxResultado;
            verTodosArtistas = artistasEncontrados.Count > maxResultado;
            verTodosPlaylist = playlistsEncontradas.Count > maxResultado;

            ResultadosMusica = new ObservableCollection<Music>(musicasEncontradas.Take(4));
            ResultadosAlbuns = new ObservableCollection<GrupoMusicas>(albunsEncontrados.Take(maxResultado));
            ResultadosArtistas = new ObservableCollection<GrupoMusicas>(artistasEncontrados.Take(maxResultado));
            ResultadosPlaylists = new ObservableCollection<PlaylistComCapa>(playlistsEncontradas.Take(maxResultado));
        }

        public void TocarMusica(Music musica)
        {
            _shell.Player.TocarMusica(musica, ResultadosMusica.ToList());
        }

        [RelayCommand]
        private void AbrirAlbum(GrupoMusicas grupo)
        {
            GrupoDetailPageViewModel detalhe = new(_shell, () => new SearchResultPageViewModel(_shell, searchString));
            detalhe.Carregar(grupo);
            _shell.CurrentPage = detalhe;
        }

        [RelayCommand]
        private void AbrirArtista(GrupoMusicas grupo)
        {
            GrupoDetailPageViewModel detalhe = new(_shell, () => new SearchResultPageViewModel(_shell, searchString));
            detalhe.Carregar(grupo);
            _shell.CurrentPage = detalhe;
        }

        [RelayCommand]
        private void AbrirPlaylist(Playlist playlist)
        {
            if (playlist == null) return;
            PlaylistDetailPageViewModel detalhe = new(_shell, () => new SearchResultPageViewModel(_shell, searchString));
            detalhe.Carregar(playlist);
            _shell.CurrentPage = detalhe;
        }

        [RelayCommand]
        private void VerMaisMusicas()
        {
            SearchMusicPageViewModel detalhe = new(_shell, () => new SearchResultPageViewModel(_shell, searchString), searchString);
            _shell.CurrentPage = detalhe;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}