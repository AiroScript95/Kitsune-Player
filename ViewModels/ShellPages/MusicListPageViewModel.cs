using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Project_Kitsune.Models;
using Project_Kitsune.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;

namespace Project_Kitsune.ViewModels.ShellPages
{
    public partial class MusicListPageViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Music> Musicas => bibliotecaService.Musicas;
        protected BibliotecaService bibliotecaService;
        protected ConfiguracaoService configuracaoService;
        public PlayerViewModel PlayerViewModel { get; set; }
        protected DatabaseService databaseService;
        protected OrdenacaoService ordenacaoService;

        public ShellViewModel Shell { get; set; }

        public string FormatoLista
        {
            get => Shell.FormatoLista;
            set
            {
                Shell.FormatoLista = value;
                OnPropertyChanged();
            }
        }

        private bool estaCarregandoMusicas;

        public bool EstaCarregandoMusicas
        {
            get => estaCarregandoMusicas;
            set
            {
                estaCarregandoMusicas = value;
                OnPropertyChanged();
            }
        }

        private string _ordemAtual = string.Empty;

        public string OrdemAtual
        {
            get => _ordemAtual;
            set
            {
                _ordemAtual = value;
                OnPropertyChanged();
            }
        }

        private bool _ordemDescendente;

        public bool OrdemDescendente
        {
            get => _ordemDescendente;
            set
            {
                _ordemDescendente = value;
                OnPropertyChanged();
            }
        }

        private Music? _musicaParaScroll;

        public Music? MusicaParaScroll
        {
            get => _musicaParaScroll;
            set
            {
                _musicaParaScroll = value;
                OnPropertyChanged();
            }
        }

        private bool _mostrarOrdenacao;

        public bool MostrarOrdenacao
        {
            get => _mostrarOrdenacao;
            set
            {
                _mostrarOrdenacao = value;
                OnPropertyChanged();
            }
        }

        public IEnumerable<string> Alfabeto { get; } = new[] { "&", "#" }.Concat(Enumerable.Range('A', 26).Select(c => ((char)c).ToString()));

        public MusicListPageViewModel(ShellViewModel shell)
        {
            Shell = shell;
            Shell.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ShellViewModel.FormatoLista))
                    OnPropertyChanged(nameof(FormatoLista));
            };
            bibliotecaService = App.ServiceProvider.GetRequiredService<BibliotecaService>();
            configuracaoService = App.ServiceProvider.GetRequiredService<ConfiguracaoService>();
            PlayerViewModel = App.ServiceProvider.GetRequiredService<PlayerViewModel>();
            databaseService = App.ServiceProvider.GetRequiredService<DatabaseService>();
            ordenacaoService = App.ServiceProvider.GetRequiredService<OrdenacaoService>();
            Configuracao config = configuracaoService.CarregarConfiguracao();
            OrdemAtual = config.OrdemMusicList;
            OrdemDescendente = config.OrdemDescendente;
            OnPropertyChanged(nameof(OrdemAtual));
            _ = CarregarMusicas();
        }

        private async Task CarregarMusicas()
        {
            await Task.Run(() => EstaCarregandoMusicas = true);
            try
            {
                Configuracao config = await Task.Run(() => configuracaoService.CarregarConfiguracao());
                bibliotecaService.BibliotecaCarregada += () =>
                {
                    System.Diagnostics.Debug.WriteLine("Biblioteca carregada!");
                    PlayerViewModel.ListaAtual = Musicas.ToList();
                    PlayerViewModel.RestaurarUltimaMusica();
                };
                await bibliotecaService.GarantirBibliotecaCarregadaAsync(config.RotaMusica);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro em CarregarMusicas: {ex}");
            }
            finally
            {
                await Task.Run(() => EstaCarregandoMusicas = false);
            }
        }

        public void TocarMusica(Music musica)
        {
            PlayerViewModel.TocarMusica(musica, Musicas.ToList());
        }

        private char NormalizarPrimeiraLetra(string? titulo)
        {
            if (string.IsNullOrEmpty(titulo)) return '\0';

            string primeiroElemento = StringInfo.GetNextTextElement(titulo);

            string decomposto;
            try
            {
                decomposto = primeiroElemento.Normalize(NormalizationForm.FormD);
            }
            catch (ArgumentException)
            {
                return char.ToUpperInvariant(titulo[0]);
            }

            foreach (char c in decomposto)
            {
                var categoria = CharUnicodeInfo.GetUnicodeCategory(c);
                if (categoria != UnicodeCategory.NonSpacingMark)
                    return char.ToUpperInvariant(c);
            }

            return char.ToUpperInvariant(titulo[0]);
        }

        public void MudarOrdenacao(string criterio, bool ordem)
        {
            Configuracao config = configuracaoService.CarregarConfiguracao();
            config.OrdemMusicList = criterio;
            config.OrdemDescendente = ordem;
            OrdemAtual = criterio;
            OrdemDescendente = ordem;
            OnPropertyChanged(nameof(OrdemAtual));
            configuracaoService.GuardarConfiguracao(config);

            List<Music> ordenadas = ordenacaoService.Ordenar(Musicas.ToList(), criterio, ordem);

            for (int novoIndice = 0; novoIndice < ordenadas.Count; novoIndice++)
            {
                Music musica = ordenadas[novoIndice];
                int indiceAtual = Musicas.IndexOf(musica);
                if (indiceAtual != novoIndice)
                    Musicas.Move(indiceAtual, novoIndice);
            }
        }

        [RelayCommand]
        private void SaltarParaLetra(string letra)
        {
            Music? alvo;

            if (letra == "#")
            {
                alvo = Musicas.FirstOrDefault(m =>
                    !string.IsNullOrEmpty(m.Titulo) && char.IsAsciiDigit(NormalizarPrimeiraLetra(m.Titulo)));
            }
            else if (letra == "&")
            {
                alvo = Musicas.FirstOrDefault(m =>
                    string.IsNullOrEmpty(m.Titulo) || (!char.IsAsciiLetter(NormalizarPrimeiraLetra(m.Titulo))
                    && !char.IsAsciiDigit(NormalizarPrimeiraLetra(m.Titulo))));
            }
            else
            {
                char letraAlvo = char.ToUpperInvariant(letra[0]);
                alvo = Musicas.FirstOrDefault(m =>
                    !string.IsNullOrEmpty(m.Titulo) && NormalizarPrimeiraLetra(m.Titulo) == letraAlvo);
            }
            if (alvo != null) MusicaParaScroll = alvo;
        }

        [RelayCommand]
        private void MostrarPainelOrdenacao()
        {
            MostrarOrdenacao = !MostrarOrdenacao;
        }

        [RelayCommand]
        private void FecharPainelOrdenacao()
        {
            MostrarOrdenacao = false;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}