using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Project_Kitsune.Models;
using Project_Kitsune.Services;
using Project_Kitsune.ViewModels.ShellPages;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Project_Kitsune.ViewModels
{
    public partial class ShellViewModel : INotifyPropertyChanged
    {
        /* ESTADOS - (Campos/Propriedades) */
        protected DatabaseService _databaService;
        public PlayerViewModel Player { get; set; }
        protected ConfiguracaoService configuracaoService;
        public string StringImageConverter { get; set; } = string.Empty;
        private string _caminhoBackground = string.Empty;

        public string CaminhoBackground
        {
            get => _caminhoBackground;
            set
            {
                _caminhoBackground = value;
                OnPropertyChanged();
            }
        }

        private string _tema = string.Empty;

        public string Tema
        {
            get => _tema;
            set
            {
                _tema = value;
                OnPropertyChanged();
            }
        }

        private object? _currentPage;

        public object? CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                OnPropertyChanged();
            }
        }

        private string _border = string.Empty;

        public string Border
        {
            get => _border;
            set { _border = value; OnPropertyChanged(); }
        }

        private string _formatoLista = "List";

        public string FormatoLista
        {
            get => _formatoLista;
            set { _formatoLista = value; OnPropertyChanged(); }
        }

        private string _page = "MusicList";

        public string Page
        {
            get => _page;
            set
            {
                _page = value;
                OnPropertyChanged();
            }
        }

        public ShellViewModel()
        {
            _databaService = App.ServiceProvider.GetRequiredService<DatabaseService>();
            Player = App.ServiceProvider.GetRequiredService<PlayerViewModel>();
            configuracaoService = App.ServiceProvider.GetRequiredService<ConfiguracaoService>();

            // Carrega a configuração e usa o CaminhoBackground guardado nela
            Configuracao config = configuracaoService.CarregarConfiguracao();
            CaminhoBackground = config.Background;
            Border = config.BorderColor;
            FormatoLista = config.FormatoLista;
            CurrentPage = new MusicListPageViewModel(this);
            AplicarMascote(config.Theme);
        }

        /* METODOS */

        public void AplicarMascote(string tema)
        {
            string caminhoMascote = tema switch
            {
                "Yoru" => "/Resources/Mascotes/yoru.svg",
                "Yako" => "/Resources/Mascotes/yako.svg",
                "Asa" => "/Resources/Mascotes/asa.svg",
                "Zenko" => "/Resources/Mascotes/zenko.svg",
                "Nogitsune" => "/Resources/Mascotes/nogitsune.svg",
                _ => "/Resources/Mascotes/asa.svg"
            };
            Tema = caminhoMascote;
        }

        /* RELAYCOMMANDS */

        [RelayCommand]
        private void IrParaMusicas()
        {
            Page = "MusicList";
            CurrentPage = new MusicListPageViewModel(this);
        }

        [RelayCommand]
        private void IrParaPlaylist()
        {
            Page = "Playlist";
            CurrentPage = new PlaylistsPageViewModel(this);
        }

        [RelayCommand]
        private void IrParaConfiguracoes()
        {
            Page = "Config";
            CurrentPage = new ConfiguracoesPageViewModel(this);
        }

        [RelayCommand]
        private void AbrirFavoritos()
        {
            Playlist? favoritos = _databaService.ListarPlaylists().FirstOrDefault(p => p.Name == "Favoritos");
            if (favoritos == null) return;
            PlaylistDetailPageViewModel detalhe = new(this);
            detalhe.Carregar(favoritos);
            Page = "Favoritos";
            CurrentPage = detalhe;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}