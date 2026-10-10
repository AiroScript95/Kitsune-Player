using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Project_Kitsune.Models;
using Project_Kitsune.Services;
using System.Globalization;
using System.Text;

namespace Project_Kitsune.ViewModels.ShellPages
{
    public partial class ArtistasPageViewModel : ObservableObject
    {
        [ObservableProperty] private List<GrupoMusicas> _artistas = new();

        private GrupoMusicas? _GrupoParaScroll;

        public GrupoMusicas? GrupoParaScroll
        {
            get => _GrupoParaScroll;
            set
            {
                _GrupoParaScroll = value;
                OnPropertyChanged();
            }
        }

        public IEnumerable<string> Alfabeto { get; } = new[] { "&", "#" }.Concat(Enumerable.Range('A', 26).Select(c => ((char)c).ToString()));

        private readonly ShellViewModel _shell;

        public ArtistasPageViewModel(ShellViewModel shell)
        {
            _shell = shell;
            _ = CarregarAsync();
        }

        private async Task CarregarAsync()
        {
            var grupos = App.ServiceProvider.GetRequiredService<GruposService>();
            Artistas = await grupos.ObterArtistasAsync();
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

        [RelayCommand]
        private void SaltarParaLetra(string letra)
        {
            GrupoMusicas? alvo;

            if (letra == "#")
            {
                alvo = Artistas.FirstOrDefault(m =>
                    !string.IsNullOrEmpty(m.Nome) && char.IsAsciiDigit(NormalizarPrimeiraLetra(m.Nome)));
            }
            else if (letra == "&")
            {
                alvo = Artistas.FirstOrDefault(m =>
                    string.IsNullOrEmpty(m.Nome) || (!char.IsAsciiLetter(NormalizarPrimeiraLetra(m.Nome))
                    && !char.IsAsciiDigit(NormalizarPrimeiraLetra(m.Nome))));
            }
            else
            {
                char letraAlvo = char.ToUpperInvariant(letra[0]);
                alvo = Artistas.FirstOrDefault(m =>
                    !string.IsNullOrEmpty(m.Nome) && NormalizarPrimeiraLetra(m.Nome) == letraAlvo);
            }
            GrupoParaScroll = null;
            if (alvo != null) GrupoParaScroll = alvo;
        }

        [RelayCommand]
        private void AbrirArtista(GrupoMusicas grupo)
        {
            GrupoDetailPageViewModel detalhe = new(_shell, () => new ArtistasPageViewModel(_shell));
            detalhe.Carregar(grupo);
            _shell.CurrentPage = detalhe;
        }
    }
}