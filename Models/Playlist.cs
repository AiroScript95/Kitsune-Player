using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project_Kitsune.Models
{
    public class Playlist
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
        public List<Music> Musics { get; set; } = new List<Music>();
        public string Ordem { get; set; } = "Manual";
        public int NumMusicas { get; set; }
    }
}