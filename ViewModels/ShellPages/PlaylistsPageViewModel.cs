using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Project_Kitsune.Models;
using Project_Kitsune.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Project_Kitsune.ViewModels.ShellPages
{
    public partial class PlaylistsPageViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<PlaylistComCapa> Playlists { get; set; } = new ObservableCollection<PlaylistComCapa>();
        protected BibliotecaService _biblioteca;

        protected DatabaseService _database;
        protected ShellViewModel _shell;

        private bool _mostrarModalCriarPlaylist;

        public bool MostrarModalCriarPlaylist
        {
            get => _mostrarModalCriarPlaylist;
            set
            {
                _mostrarModalCriarPlaylist = value;
                OnPropertyChanged();
            }
        }

        private string _novaPlaylist = string.Empty;

        public string NovaPlaylist
        {
            get => _novaPlaylist;
            set
            {
                _novaPlaylist = value;
                OnPropertyChanged();
            }
        }

        private bool mostrarConfirmacaoExclusao;

        public bool MostrarConfirmacaoExclusao
        {
            get => mostrarConfirmacaoExclusao;
            set
            {
                mostrarConfirmacaoExclusao = value;
                OnPropertyChanged();
            }
        }

        public PlaylistsPageViewModel(ShellViewModel shell)
        {
            _database = App.ServiceProvider.GetRequiredService<DatabaseService>();
            _biblioteca = App.ServiceProvider.GetRequiredService<BibliotecaService>();
            _shell = shell;

            List<Playlist> existentes = _database.ListarPlaylists();
            foreach (Playlist p in existentes)
            {
                Playlists.Add(CarregarComCapa(p));
            }
        }

        private PlaylistComCapa CarregarComCapa(Playlist playlist)
        {
            // Favoritos não usa PlaylistMusicas — usa a coluna Gosto na tabela Musicas
            List<string> todosCaminhos = playlist.Name == "Favoritos"
                ? _database.ListarMusicasFavoritas()
                : _database.ListarMusicasDaPlaylist(playlist.Id);

            string? caminho = todosCaminhos.FirstOrDefault();
            byte[]? capa = null;

            if (caminho != null)
            {
                Music? musica = _biblioteca.LerMusicaPorCaminho(caminho);
                if (musica != null)
                {
                    capa = musica.Image;
                }
            }
            List<string> listaMusicas = todosCaminhos
                .Take(3)
                .Select(caminho => _biblioteca.LerMusicaPorCaminho(caminho)?.Titulo ?? System.IO.Path.GetFileNameWithoutExtension(caminho))
                .ToList();

            if (playlist.Name == "Favoritos")
            {
                playlist.NumMusicas = todosCaminhos.Count;
            }

            return new PlaylistComCapa(playlist, capa, listaMusicas);
        }

        [RelayCommand]
        private void MostrarModal()
        {
            MostrarModalCriarPlaylist = true;
        }

        [RelayCommand]
        private void CriarPlaylist()
        {
            if (string.IsNullOrWhiteSpace(NovaPlaylist)) return;

            _database.CriarPlaylist(NovaPlaylist);

            List<Playlist> atualizadas = _database.ListarPlaylists();
            Playlists.Clear();
            foreach (Playlist p in atualizadas)
            {
                Playlists.Add(CarregarComCapa(p));
            }

            NovaPlaylist = string.Empty;
            MostrarModalCriarPlaylist = false;
        }

        [RelayCommand]
        private void FecharModal()
        {
            NovaPlaylist = string.Empty;
            MostrarModalCriarPlaylist = false;
        }

        [RelayCommand]
        private void RemoverPlaylist(Playlist playlist)
        {
            // Impede apagar a playlist de sistema
            if (playlist.Name == "Favoritos") return;

            _database.RemoverPlaylist(playlist.Id);
            PlaylistComCapa? item = Playlists.FirstOrDefault(p => p.Playlist.Id == playlist.Id);
            if (item != null) Playlists.Remove(item);
        }

        [RelayCommand]
        public void AbrirPlaylist(Playlist playlist)
        {
            PlaylistDetailPageViewModel detalhe = new(_shell);
            detalhe.Carregar(playlist);
            _shell.CurrentPage = detalhe;
        }

        private Playlist? _playlistParaExcluir;

        [RelayCommand]
        private void PedirExclusao(Playlist playlist)
        {
            _playlistParaExcluir = playlist;
            MostrarConfirmacaoExclusao = true;
        }

        [RelayCommand]
        private void ConfirmarExclusao()
        {
            if (_playlistParaExcluir != null)
                RemoverPlaylist(_playlistParaExcluir);
            MostrarConfirmacaoExclusao = false;
            _playlistParaExcluir = null;
        }

        [RelayCommand]
        private void CancelarExclusao()
        {
            MostrarConfirmacaoExclusao = false;
            _playlistParaExcluir = null;
        }

        public class PlaylistComCapa
        {
            public Playlist Playlist { get; set; }
            public byte[]? Capa { get; set; }
            public List<string> ListaMusicas { get; set; }

            public PlaylistComCapa(Playlist playlist, byte[]? capa, List<string> listaMusicas)
            {
                Playlist = playlist;
                Capa = capa;
                ListaMusicas = listaMusicas;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}