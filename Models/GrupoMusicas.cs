using CommunityToolkit.Mvvm.Messaging;
using Project_Kitsune.Services;
using Project_Kitsune.ViewModels;

namespace Project_Kitsune.Models
{
    public class GrupoMusicas
    {
        public string Nome { get; set; } = "";
        public byte[]? Capa { get; set; }
        public List<string> ListaMusicas { get; set; } = new();   // 3 títulos para o cartão
        public List<Music> Musicas { get; set; } = new();
        public int NumMusicas => Musicas.Count;
    }
}