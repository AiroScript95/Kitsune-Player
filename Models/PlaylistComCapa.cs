using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_Kitsune.Models
{
    public class PlaylistComCapa
    {
        public Playlist Playlist { get; set; }
        public byte[]? Capa { get; set; }
        public List<string> ListaMusicas { get; set; }
        public bool MenuOpen { get; set; }

        public PlaylistComCapa(Playlist playlist, byte[]? capa, List<string> listaMusicas, bool menuOpen)
        {
            Playlist = playlist;
            Capa = capa;
            ListaMusicas = listaMusicas;
            MenuOpen = menuOpen;
        }
    }
}