using Project_Kitsune.Models;

namespace Project_Kitsune.Services
{
    public class OrdenacaoService(DatabaseService database)
    {
        protected DatabaseService _database = database;
        private static readonly Random _random = new Random();

        public List<Music> Ordenar(List<Music> lista, string criterio, bool ordemDesc)
        {
            if (lista == null) return [];

            return criterio switch
            {
                "Alfabetica" => ordemDesc ? lista.OrderByDescending(m => m.Titulo).ToList() : lista.OrderBy(m => m.Titulo).ToList(),
                "MaisRecente" => ordemDesc ? lista.OrderByDescending(m => m.DataAdicionado).ToList() : lista.OrderBy(m => m.DataAdicionado).ToList(),
                "MaisTocada" => ordemDesc ? lista.OrderByDescending(m => m.VezesTocada).ToList() : lista.OrderBy(m => m.VezesTocada).ToList(),
                "Duracao" => ordemDesc ? lista.OrderByDescending(m => m.Duracao).ToList() : lista.OrderBy(m => m.Duracao).ToList(),
                "Aleatorio" => Shuffle(lista),
                _ => lista
            };
        }

        private static List<T> Shuffle<T>(List<T> list)
        {
            var copy = list.ToList();
            for (int i = copy.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (copy[i], copy[j]) = (copy[j], copy[i]);
            }
            return copy;
        }
    }
}