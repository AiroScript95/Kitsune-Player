using System.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Project_Kitsune.Models;
using Project_Kitsune.Services;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace Project_Kitsune.ViewModels.ShellPages
{
    public partial class ConfiguracoesPageViewModel : INotifyPropertyChanged
    {
        public string PastaPadrao { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        public ObservableCollection<string> PastasAdicionais { get; set; } = new ObservableCollection<string>();
        protected ShellViewModel Shell;
        private readonly BibliotecaService bibliotecaService;
        private bool _temImage;

        public bool TemImage
        {
            get => _temImage;
            set
            {
                _temImage = value;
                OnPropertyChanged();
            }
        }

        private string _caminhoBackground = string.Empty;

        public string ImagemBackground
        {
            get => _caminhoBackground;
            set
            {
                _caminhoBackground = value;
                OnPropertyChanged();
            }
        }

        private ConfiguracaoService configuracaoService;
        private string _temaSelecionado = string.Empty;

        public string TemaSelecionado
        {
            get => _temaSelecionado;
            set
            {
                _temaSelecionado = value;
                OnPropertyChanged();

                if (!string.IsNullOrEmpty(value))
                {
                    configuracaoService.AtualizarTema(value);
                    ((App)Application.Current).AplicarTema(value);
                    Shell.AplicarMascote(value);
                }
            }
        }

        public record IdiomaItem(string Codigo, string Nome);

        public List<IdiomaItem> IdiomasDisponiveis { get; } = new List<IdiomaItem>()
        {
            new("pt", "Português"),
            new("en", "English"),
            new("jp", "日本語"),
            new("es", "Español")
        };

        private string _idiomaSelecionado = string.Empty;

        public string IdiomaSelecionado
        {
            get => _idiomaSelecionado;
            set
            {
                _idiomaSelecionado = value;
                OnPropertyChanged();
                if (!string.IsNullOrEmpty(value))
                {
                    configuracaoService.AtualizarIdiona(value);
                    ((App)Application.Current).AplicarIdioma(value);
                }
            }
        }

        public ConfiguracoesPageViewModel(ShellViewModel shell)
        {
            Shell = shell;
            bibliotecaService = App.ServiceProvider.GetRequiredService<BibliotecaService>();
            configuracaoService = App.ServiceProvider.GetRequiredService<ConfiguracaoService>();
            Configuracao config = configuracaoService.CarregarConfiguracao();
            _temaSelecionado = string.IsNullOrEmpty(config.Theme) ? "Asa" : config.Theme;
            _idiomaSelecionado = string.IsNullOrEmpty(config.Idioma) ? App.DetectarIdiomaSistema() : config.Idioma;
            foreach (string pasta in config.RotaMusica)
            {
                if (pasta != PastaPadrao)
                {
                    PastasAdicionais.Add(pasta);
                }
            }
            ImagemBackground = config.Background;
            TemImage = !string.IsNullOrEmpty(config.Background);
        }

        [RelayCommand]
        private async Task AdicionarPasta()
        {
            OpenFolderDialog dialog = new OpenFolderDialog();
            bool? resultado = dialog.ShowDialog();

            if (resultado == true)
            {
                string caminhoEscolhido = dialog.FolderName;
                bool sucesso = configuracaoService.AdicionarPastaMusica(caminhoEscolhido);

                if (sucesso)
                {
                    PastasAdicionais.Add(caminhoEscolhido);
                    Configuracao config = configuracaoService.CarregarConfiguracao();
                    await bibliotecaService.GarantirBibliotecaCarregadaAsync(config.RotaMusica, forcar: true);
                }
            }
        }

        [RelayCommand]
        private async Task RemoverPasta(string caminho)
        {
            configuracaoService.RemoverPastaMusica(caminho);
            PastasAdicionais.Remove(caminho);
            Configuracao config = configuracaoService.CarregarConfiguracao();
            await bibliotecaService.GarantirBibliotecaCarregadaAsync(config.RotaMusica, forcar: true);
        }

        [RelayCommand]
        private void IrParaTema()
        {
            Shell.CurrentPage = new ConfigTemaPageViewModel(Shell);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}