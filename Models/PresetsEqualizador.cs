using System.Collections.Generic;

namespace Project_Kitsune.Models
{
    // Molde cru para desserializar o JSON externo
    public class EqPresetsFile
    {
        public int BandCount { get; set; }
        public List<double> FrequenciesHz { get; set; } = new();
        public List<EqPresetJson> Presets { get; set; } = new();
    }

    public class EqPresetJson
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public List<double> ValuesDb { get; set; } = new();
    }

    // Modelo usado pelo resto da app
    public class PresetEqualizador
    {
        public uint Indice { get; set; }
        public string Nome { get; set; } = "";
        public List<BandaEq> Bandas { get; set; } = new();
    }

    public class BandaEq
    {
        public double Frequencia { get; set; }
        public double GanhoDb { get; set; }
    }
}