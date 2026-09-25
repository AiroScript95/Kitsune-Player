using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace Project_Kitsune.Models
{
    // Representa uma única linha de letra com timestamp
    public class LinhaLetra
    {
        public TimeSpan Tempo { get; set; }
        public string Texto { get; set; } = string.Empty;
    }

    public enum FonteLetra
    {
        Nenhuma,
        Embutida,
        ArquivoLrc,
        Lrclib
    }

    public class Letra
    {
        public string ConteudoBruto { get; set; } = string.Empty;
        public List<LinhaLetra> Linhas { get; set; } = new List<LinhaLetra>();
        public string? CaminhoOrigem { get; set; }
        public string? Idioma { get; set; }
        public FonteLetra Fonte { get; set; }
        public bool TemTimestampsReais { get; set; }
        public bool Sincronizada => Linhas.Count > 0 && TemTimestampsReais;
    }

    public class Music : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Caminho { get; set; } = string.Empty;

        private string _titulo = string.Empty;

        public string Titulo
        {
            get => _titulo;
            set { _titulo = value; OnPropertyChanged(); }
        }

        private string _artista = string.Empty;

        public string Artista
        {
            get => _artista;
            set { _artista = value; OnPropertyChanged(); }
        }

        private string _album = string.Empty;

        public string Album
        {
            get => _album;
            set { _album = value; OnPropertyChanged(); }
        }

        private bool _temLetraDisponivel;

        public bool TemLetraDisponivel
        {
            get => _temLetraDisponivel;
            set
            {
                _temLetraDisponivel = value;
                OnPropertyChanged();
            }
        }

        public string Genero { get; set; } = string.Empty;
        public TimeSpan Duracao { get; set; }
        public TimeSpan DuracaoArredondada { get; set; }
        public DateTime DataAdicionado { get; set; }
        public int VezesTocada { get; set; }
        public byte[] Image { get; set; } = Array.Empty<byte>();
        public bool Gosto { get; set; }
        public Letra? LetraAtual { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}