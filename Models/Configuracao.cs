namespace Project_Kitsune.Models
{
    public class UltimaMusicaInfo
    {
        public string? Caminho { get; set; }
        public long PosicaoMs { get; set; } = 0;
        public long DuracaoMS { get; set; } = 0;
        public bool EstavaTocando { get; set; } = false;
        public DateTime SalvoEm { get; set; } = DateTime.UtcNow;
    }

    public class Configuracao
    {
        public string Idioma { get; set; } = string.Empty;
        public string Theme { get; set; } = string.Empty;
        public string OrdemMusicList { get; set; } = "Alfabetica";
        public string FormatoLista { get; set; } = "List";
        public string ModoRepeticao { get; set; } = "Nenhum";
        public string Background { get; set; } = string.Empty;
        public string BorderColor { get; set; } = "Color2";
        public double JanelaLargura { get; set; }
        public double JanelaAltura { get; set; }
        public double JanelaX { get; set; }
        public double JanelaY { get; set; }
        public double MiniJanelaX { get; set; }
        public double MiniJanelaY { get; set; }
        public int EqualizadorPreset { get; set; } = 0;
        public float[]? EqualizadorGanhosCustom { get; set; } = null;
        public List<string> RotaMusica { get; set; } = new List<string>();
        public UltimaMusicaInfo? UltimaMusica { get; set; }
        public bool OrdemDescendente { get; set; } = false;
    }
}