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

namespace Project_Kitsune.ViewModels.ShellPages.SearchPages
{
    public partial class SearchMusicPageViewModel : INotifyPropertyChanged
    {
        private readonly ShellViewModel _shell;
        public string searchString { get; }
        private readonly BibliotecaService _biblioteca;
        public PlayerViewModel PlayerViewModel { get; }
        private Func<object> _voltarPara;

        private ObservableCollection<Music> _resultadosMusica = new();

        public ObservableCollection<Music> ResultadosMusica {
            get => _resultadosMusica;
            private set { _resultadosMusica = value; OnPropertyChanged(); }
        }

        public SearchMusicPageViewModel(ShellViewModel shell, Func<object> voltar, string resultado)
        {
            _shell = shell;
            searchString = resultado;
            _voltarPara = voltar;
            _biblioteca = App.ServiceProvider.GetRequiredService<BibliotecaService>();
            PlayerViewModel = App.ServiceProvider.GetRequiredService<PlayerViewModel>();
            Pesquisar();
        }

        private void Pesquisar()
        {
            string termo = searchString.Trim();
            if (termo.Length == 0) {
                ResultadosMusica = new();
                return;
            }

            var musicasEncontradas = _biblioteca.Musicas.ToList().Where(m =>
                                            ( m.Titulo?.Contains(termo, StringComparison.OrdinalIgnoreCase) ?? false ) ||
                                            ( m.Artista?.Contains(termo, StringComparison.OrdinalIgnoreCase) ?? false ) ||
                                            ( m.Album?.Contains(termo, StringComparison.OrdinalIgnoreCase) ?? false )).ToList();

            ResultadosMusica = new ObservableCollection<Music>(musicasEncontradas);
        }

        public void TocarMusica(Music musica)
        {
            _shell.Player.TocarMusica(musica, ResultadosMusica.ToList());
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}