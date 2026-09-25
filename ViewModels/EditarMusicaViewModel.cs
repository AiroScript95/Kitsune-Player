using CommunityToolkit.Mvvm.Input;
using Project_Kitsune.Models;
using Project_Kitsune.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_Kitsune.ViewModels
{
    public partial class EditarMusicaViewModel : INotifyPropertyChanged
    {
        private readonly Music _musicaOriginal;
        private readonly BibliotecaService _biblioteca; // ou onde o SalvarEdicaoMusica mora

        public string Titulo { get; set; }
        public string Artista { get; set; }
        public string Album { get; set; }

        public event Action? FechadoComSucesso;

        public EditarMusicaViewModel(Music musica, BibliotecaService biblioteca)
        {
            _musicaOriginal = musica;
            _biblioteca = biblioteca;
            Titulo = musica.Titulo;
            Artista = musica.Artista;
            Album = musica.Album;
        }

        [RelayCommand]
        private void Salvar()
        {
            _biblioteca.SalvarEdicaoMusica(_musicaOriginal, Titulo, Artista, Album);
            FechadoComSucesso?.Invoke();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}