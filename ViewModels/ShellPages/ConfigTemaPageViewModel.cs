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
    public partial class ConfigTemaPageViewModel : INotifyPropertyChanged
    {
        protected ShellViewModel _shell;
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

        public string Border
        {
            get => _shell.Border;
            set { _shell.Border = value; OnPropertyChanged(); }
        }

        public string FormatoLista
        {
            get => _shell.FormatoLista;
            set
            {
                _shell.FormatoLista = value;
                OnPropertyChanged();
            }
        }

        protected ConfiguracaoService configuracaoService;
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
                    _shell.AplicarMascote(value);
                }
            }
        }

        public ConfigTemaPageViewModel(ShellViewModel shell)
        {
            _shell = shell;
            configuracaoService = App.ServiceProvider.GetRequiredService<ConfiguracaoService>();
            Configuracao config = configuracaoService.CarregarConfiguracao();
            _temaSelecionado = string.IsNullOrEmpty(config.Theme) ? "Asa" : config.Theme;
            ImagemBackground = config.Background;
            //Border = config.BorderColor;
            TemImage = !string.IsNullOrEmpty(config.Background);
        }

        public void MudarBorder(string SelectBorder)
        {
            Configuracao config = configuracaoService.CarregarConfiguracao();
            _shell.Border = SelectBorder;
            config.BorderColor = _shell.Border;
            OnPropertyChanged(nameof(Border));
            configuracaoService.GuardarConfiguracao(config);
        }

        public void MudarFormato(string formato)
        {
            Configuracao config = configuracaoService.CarregarConfiguracao();
            _shell.FormatoLista = formato;
            config.FormatoLista = formato;
            configuracaoService.GuardarConfiguracao(config);
            OnPropertyChanged(nameof(FormatoLista));
        }

        [RelayCommand]
        private void AdicionarBackground()
        {
            OpenFileDialog dialog = new OpenFileDialog()
            {
                Filter = "Imagens|*.png;*.jpg;*.jpeg;"
            };
            bool? resultado = dialog.ShowDialog();
            if (resultado == true)
            {
                string caminho = dialog.FileName;
                configuracaoService.GuardarBackground(caminho);
                Configuracao configuracao = configuracaoService.CarregarConfiguracao();
                _shell.CaminhoBackground = configuracao.Background;
                ImagemBackground = configuracao.Background;
                TemImage = true;
            }
        }

        [RelayCommand]
        private void RemoverBackground()
        {
            configuracaoService.RemoverBackground();

            _shell.CaminhoBackground = string.Empty;
            ImagemBackground = string.Empty;
            TemImage = false;
        }

        [RelayCommand]
        private void Voltar()
        {
            _shell.CurrentPage = new ConfiguracoesPageViewModel(_shell);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}