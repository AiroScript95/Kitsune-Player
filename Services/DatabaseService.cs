using Microsoft.Data.Sqlite;
using Project_Kitsune.Models;
using System.IO;

namespace Project_Kitsune.Services
{
    public class DatabaseService
    {
        /* ESTADO */
        private string _database = string.Empty;

        public DatabaseService()
        {
            string caminhoData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string caminhoPasta = Path.Combine(caminhoData, "Kitsune");
            string caminhoPastaConfig = Path.Combine(caminhoPasta, "Settings");
            Directory.CreateDirectory(caminhoPastaConfig);
            string caminhoDb = Path.Combine(caminhoPastaConfig, "Kitsune.db");
            _database = $"Data Source={caminhoDb}";
        }

        /* METODOS */

        public void IniciarDatabase()
        {
            string comandoMusicas = @"
            CREATE TABLE IF NOT EXISTS Musicas (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Caminho TEXT NOT NULL UNIQUE,
                Titulo TEXT,
                Artista TEXT,
                Album TEXT,
                Genero TEXT,
                DuracaoSegundos INTEGER,
                DataModificacao INTEGER NOT NULL,
                Imagem BLOB,
                PrimeiraVezAusente TEXT,
                VezesTocada INTEGER DEFAULT 0,
                Gosto INTEGER DEFAULT 0,
                DataGosto TEXT
            )";

            string comandoPlaylists = @"
                CREATE TABLE IF NOT EXISTS Playlists (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nome TEXT NOT NULL,
                DataCriacao TEXT,
                Ordem TEXT DEFAULT 'Manual'
            )";
            string comandoPlaylistMusicas = @"
                 CREATE TABLE IF NOT EXISTS PlaylistMusicas (
                 PlaylistId INTEGER,
                 MusicaId INTEGER,
                 DataAdicionado TEXT,
                 FOREIGN KEY (PlaylistId) REFERENCES Playlists(Id),
                 FOREIGN KEY (MusicaId) REFERENCES Musicas(Id),
                 UNIQUE(PlaylistId, MusicaId)
             )";

            using (SqliteConnection connection = new(_database))
            {
                connection.Open();

                SqliteCommand cmd = connection.CreateCommand();

                cmd.CommandText = "PRAGMA journal_mode=WAL;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = comandoMusicas;
                cmd.ExecuteNonQuery();

                cmd.CommandText = comandoPlaylists;
                cmd.ExecuteNonQuery();

                cmd.CommandText = comandoPlaylistMusicas;
                cmd.ExecuteNonQuery();
            }
            GarantirPlaylistFavoritos();
        }

        public List<string> ListarMusicasDaPlaylist(int playlistId)
        {
            List<string> caminhos = new List<string>();

            using (SqliteConnection connection = new(_database))
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT Musicas.Caminho
                    FROM Musicas
                    INNER JOIN PlaylistMusicas ON Musicas.Id = PlaylistMusicas.MusicaId
                    WHERE PlaylistMusicas.PlaylistId = @playlistId
                    ORDER BY PlaylistMusicas.DataAdicionado DESC";
                cmd.Parameters.AddWithValue("@playlistId", playlistId);

                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        caminhos.Add(reader.GetString(0));
                    }
                }
            }

            return caminhos;
        }

        public List<(int Id, string Caminho)> ListarTodasMusicas()
        {
            List<(int Id, string Caminho)> musicas = new List<(int Id, string Caminho)>();

            using (SqliteConnection connection = new(_database))
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT Id, Caminho FROM Musicas";

                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        musicas.Add((reader.GetInt32(0), reader.GetString(1)));
                    }
                }
            }

            return musicas;
        }

        public List<Playlist> ListarPlaylists()
        {
            List<Playlist> playlists = new List<Playlist>();
            Dictionary<int, int> contagens = ContarMusicasPorPlaylist();

            using (SqliteConnection connection = new(_database))
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT Id, Nome, DataCriacao, Ordem FROM Playlists";

                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Playlist playlist = new Playlist();
                        playlist.Id = reader.GetInt32(0);
                        playlist.Name = reader.GetString(1);
                        playlist.DataCriacao = DateTime.Parse(reader.GetString(2));
                        playlist.Ordem = reader.GetString(3);
                        playlist.NumMusicas = contagens.TryGetValue(playlist.Id, out int qtd) ? qtd : 0;
                        playlists.Add(playlist);
                    }
                }
            }
            return playlists;
        }

        public Dictionary<int, int> ContarMusicasPorPlaylist()
        {
            var resultado = new Dictionary<int, int>();

            using (SqliteConnection connection = new(_database))
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = @"
                SELECT PlaylistId, COUNT(*)
                FROM PlaylistMusicas
                GROUP BY PlaylistId";

                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int playlistId = reader.GetInt32(0);
                        int quantidade = reader.GetInt32(1);
                        resultado[playlistId] = quantidade;
                    }
                }
            }

            return resultado;
        }

        public void RemoverPlaylist(int playlistId)
        {
            using (SqliteConnection connection = new(_database))
            {
                connection.Open();

                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "DELETE FROM PlaylistMusicas WHERE PlaylistId = @id";
                cmd.Parameters.AddWithValue("@id", playlistId);
                cmd.ExecuteNonQuery();

                cmd.CommandText = "DELETE FROM Playlists WHERE Id = @id";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@id", playlistId);
                cmd.ExecuteNonQuery();
            }
        }

        public void AdicionarMusicaAPlaylist(int playlistId, int musicaId)
        {
            using (SqliteConnection connection = new(_database))
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "INSERT OR IGNORE INTO PlaylistMusicas (PlaylistId, MusicaId, DataAdicionado) VALUES (@playlistId, @musicaId, @data)";
                cmd.Parameters.AddWithValue("@playlistId", playlistId);
                cmd.Parameters.AddWithValue("@musicaId", musicaId);
                cmd.Parameters.AddWithValue("@data", DateTime.Now.ToString());
                cmd.ExecuteNonQuery();
            }
        }

        public void RemoverMusicaPorId(int musicaId)
        {
            using (SqliteConnection connection = new(_database))
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();

                cmd.CommandText = "DELETE FROM PlaylistMusicas WHERE MusicaId = @id";
                cmd.Parameters.AddWithValue("@id", musicaId);
                cmd.ExecuteNonQuery();

                cmd.CommandText = "DELETE FROM Musicas WHERE Id = @id";
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@id", musicaId);
                cmd.ExecuteNonQuery();
            }
        }

        public void LimparMusicasOrfas()
        {
            List<(int Id, string Caminho)> todas = ListarTodasMusicas();

            foreach (var (Id, Caminho) in todas)
            {
                bool existe = File.Exists(Caminho);

                if (existe)
                {
                    LimparAusencia(Id);
                }
                else
                {
                    string? primeiraAusencia = ObterPrimeiraAusencia(Id);

                    if (primeiraAusencia == null)
                    {
                        MarcarComoAusente(Id);
                    }
                    else
                    {
                        DateTime data = DateTime.Parse(primeiraAusencia);
                        if ((DateTime.Now - data).TotalDays >= 180)
                        {
                            RemoverMusicaPorId(Id);
                        }
                    }
                }
            }
        }

        private void MarcarComoAusente(int musicaId)
        {
            using (SqliteConnection connection = new(_database))
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "UPDATE Musicas SET PrimeiraVezAusente = @data WHERE Id = @id";
                cmd.Parameters.AddWithValue("@data", DateTime.Now.ToString());
                cmd.Parameters.AddWithValue("@id", musicaId);
                cmd.ExecuteNonQuery();
            }
        }

        private void LimparAusencia(int musicaId)
        {
            using (SqliteConnection connection = new(_database))
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "UPDATE Musicas SET PrimeiraVezAusente = NULL WHERE Id = @id AND PrimeiraVezAusente IS NOT NULL";
                cmd.Parameters.AddWithValue("@id", musicaId);
                cmd.ExecuteNonQuery();
            }
        }

        public void AtualizarOrdemPlaylist(int playlistId, string ordem)
        {
            using (SqliteConnection connection = new(_database))
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "UPDATE Playlists SET Ordem = @ordem WHERE Id = @id";
                cmd.Parameters.AddWithValue("@ordem", ordem);
                cmd.Parameters.AddWithValue("@id", playlistId);
                cmd.ExecuteNonQuery();
            }
        }

        public void RemoverMusicaDaPlaylist(int playlistId, int musicaId)
        {
            using (SqliteConnection connection = new(_database))
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "DELETE FROM PlaylistMusicas WHERE PlaylistId = @playlistId AND MusicaId = @musicaId";
                cmd.Parameters.AddWithValue("@playlistId", playlistId);
                cmd.Parameters.AddWithValue("@musicaId", musicaId);
                cmd.ExecuteNonQuery();
            }
        }

        public void IncrementarReproducoes(string caminho)
        {
            using (SqliteConnection connection = new(_database))
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "UPDATE Musicas SET VezesTocada = VezesTocada + 1 WHERE Caminho = @caminho";
                cmd.Parameters.AddWithValue("@caminho", caminho);
                cmd.ExecuteNonQuery();
            }
        }

        public bool CriarPlaylist(string playlist)
        {
            using (SqliteConnection connection = new(_database))
            {
                connection.Open();

                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "INSERT INTO Playlists (Nome, DataCriacao) VALUES (@nome, @data)";
                cmd.Parameters.AddWithValue("@nome", playlist);
                cmd.Parameters.AddWithValue("@data", DateTime.Now.ToString());
                cmd.ExecuteNonQuery();
                return true;
            }
        }

        public int ObterIdMusicaPorCaminho(string caminho)
        {
            using (SqliteConnection connection = new(_database))
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT Id FROM Musicas WHERE Caminho = @caminho";
                cmd.Parameters.AddWithValue("@caminho", caminho);

                object? resultado = cmd.ExecuteScalar();
                return resultado != null ? Convert.ToInt32(resultado) : -1;
            }
        }

        public int ObterVezesTocada(string caminho)
        {
            using (SqliteConnection connection = new(_database))
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT VezesTocada FROM Musicas WHERE Caminho = @caminho";
                cmd.Parameters.AddWithValue("@caminho", caminho);
                object? resultado = cmd.ExecuteScalar();
                return resultado != null ? Convert.ToInt32(resultado) : 0;
            }
        }

        private string? ObterPrimeiraAusencia(int musicaId)
        {
            using (SqliteConnection connection = new(_database))
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT PrimeiraVezAusente FROM Musicas WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", musicaId);
                object? resultado = cmd.ExecuteScalar();
                return resultado as string;
            }
        }

        public string? ObterCaminhoMusicaMaisRecente(int playlistId)
        {
            using (SqliteConnection connection = new(_database))
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT Musicas.Caminho
                    FROM Musicas
                    INNER JOIN PlaylistMusicas ON Musicas.Id = PlaylistMusicas.MusicaId
                    WHERE PlaylistMusicas.PlaylistId = @playlistId
                    ORDER BY PlaylistMusicas.DataAdicionado DESC
                    LIMIT 1";
                cmd.Parameters.AddWithValue("@playlistId", playlistId);

                object? resultado = cmd.ExecuteScalar();
                return resultado?.ToString();
            }
        }

        public void SalvarMusicaCompleta(Music musica, long dataModificacao)
        {
            using (SqliteConnection connection = new(_database))
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = @"
                INSERT INTO Musicas (Caminho, Titulo, Artista, Album, Genero, DuracaoSegundos, DataModificacao, Imagem,Gosto)
                VALUES (@caminho, @titulo, @artista, @album, @genero, @duracao, @dataMod, @imagem,@gosto)
                ON CONFLICT(Caminho) DO UPDATE SET
                Titulo = @titulo,
                Artista = @artista,
                Album = @album,
                Genero = @genero,
                DuracaoSegundos = @duracao,
                DataModificacao = @dataMod,
                Imagem = @imagem,
                Gosto = @gosto";

                cmd.Parameters.AddWithValue("@caminho", musica.Caminho);
                cmd.Parameters.AddWithValue("@titulo", musica.Titulo);
                cmd.Parameters.AddWithValue("@artista", musica.Artista);
                cmd.Parameters.AddWithValue("@album", musica.Album);
                cmd.Parameters.AddWithValue("@genero", string.IsNullOrEmpty(musica.Genero) ? (object)DBNull.Value : musica.Genero);
                cmd.Parameters.AddWithValue("@duracao", (int)musica.Duracao.TotalSeconds);
                cmd.Parameters.AddWithValue("@dataMod", dataModificacao);
                cmd.Parameters.AddWithValue("@imagem", musica.Image.Length > 0 ? musica.Image : DBNull.Value);
                cmd.Parameters.AddWithValue("@gosto", musica.Gosto ? 1 : 0);
                cmd.ExecuteNonQuery();
            }
        }

        public (Music? Musica, long DataModificacao) ObterMusicaCache(string caminho)
        {
            using (SqliteConnection connection = new(_database))
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = @"SELECT Titulo, Artista, Album, Genero, DuracaoSegundos, DataModificacao, Imagem, Gosto
                                    FROM Musicas WHERE Caminho = @caminho";
                cmd.Parameters.AddWithValue("@caminho", caminho);

                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int duracaoSegundos = reader.GetInt32(4);
                        long dataMod = reader.GetInt64(5);
                        byte[] imagem = reader.IsDBNull(6) ? Array.Empty<byte>() : (byte[])reader["Imagem"];

                        Music musica = new Music
                        {
                            Caminho = caminho,
                            Titulo = reader.GetString(0),
                            Artista = reader.GetString(1),
                            Album = reader.GetString(2),
                            Genero = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            Duracao = TimeSpan.FromSeconds(duracaoSegundos),
                            DuracaoArredondada = TimeSpan.FromSeconds(duracaoSegundos),
                            Image = imagem,
                            Gosto = reader.GetInt32(7) == 1
                        };

                        return (musica, dataMod);
                    }
                }
            }
            return (null, 0);
        }

        public Dictionary<string, (Music Musica, long DataModificacao)> ObterTodoCache()
        {
            var resultado = new Dictionary<string, (Music, long)>();
            using (SqliteConnection connection = new(_database))
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = @"SELECT Caminho, Titulo, Artista, Album, Genero, DuracaoSegundos,
                          DataModificacao, VezesTocada, Imagem, Gosto
                          FROM Musicas";
                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string caminho = reader.GetString(0);
                        int duracaoSegundos = reader.GetInt32(5);
                        long dataMod = reader.GetInt64(6);
                        byte[] imagem = reader.IsDBNull(8) ? Array.Empty<byte>() : (byte[])reader["Imagem"];  // corrigido: 7 → 8
                        Music musica = new Music
                        {
                            Caminho = caminho,
                            Titulo = reader.GetString(1),
                            Artista = reader.GetString(2),
                            Album = reader.GetString(3),
                            Genero = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                            Duracao = TimeSpan.FromSeconds(duracaoSegundos),
                            DuracaoArredondada = TimeSpan.FromSeconds(duracaoSegundos),
                            VezesTocada = reader.GetInt32(7),
                            Image = imagem,
                            Gosto = reader.GetInt32(9) == 1
                        };
                        resultado[caminho] = (musica, dataMod);
                    }
                }
            }
            return resultado;
        }

        public void AlternarGosto(string caminho, bool gosto)
        {
            using (SqliteConnection connection = new(_database))
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "UPDATE Musicas SET Gosto = @gosto, DataGosto = @data WHERE Caminho = @caminho";
                cmd.Parameters.AddWithValue("@gosto", gosto ? 1 : 0);
                cmd.Parameters.AddWithValue("@data", gosto ? DateTime.Now.ToString() : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@caminho", caminho);
                cmd.ExecuteNonQuery();
            }
        }

        public void GarantirPlaylistFavoritos()
        {
            using (SqliteConnection connection = new(_database))
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = @"
            INSERT INTO Playlists (Nome, DataCriacao)
            SELECT @nome, @data
            WHERE NOT EXISTS (SELECT 1 FROM Playlists WHERE Nome = @nome)";
                cmd.Parameters.AddWithValue("@nome", "Favoritos");
                cmd.Parameters.AddWithValue("@data", DateTime.Now.ToString());
                cmd.ExecuteNonQuery();
            }
        }

        public List<string> ListarMusicasFavoritas()
        {
            List<string> caminhos = new List<string>();
            using (SqliteConnection connection = new(_database))
            {
                connection.Open();
                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT Caminho FROM Musicas WHERE Gosto = 1 ORDER BY DataGosto DESC";
                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) caminhos.Add(reader.GetString(0));
                }
            }
            return caminhos;
        }

        public void SalvarMusicasEmLote(List<(Music Musica, long DataModificacao)> musicas)
        {
            using (SqliteConnection connection = new(_database))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    SqliteCommand cmd = connection.CreateCommand();
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"
                INSERT INTO Musicas (Caminho, Titulo, Artista, Album, Genero, DuracaoSegundos, DataModificacao, Imagem, Gosto)
                VALUES (@caminho, @titulo, @artista, @album, @genero, @duracao, @dataMod, @imagem, @gosto)
                ON CONFLICT(Caminho) DO UPDATE SET
                    Titulo = @titulo, Artista = @artista, Album = @album, Genero = @genero,
                    DuracaoSegundos = @duracao, DataModificacao = @dataMod, Imagem = @imagem, Gosto = @gosto";

                    var pCaminho = cmd.Parameters.Add("@caminho", SqliteType.Text);
                    var pTitulo = cmd.Parameters.Add("@titulo", SqliteType.Text);
                    var pArtista = cmd.Parameters.Add("@artista", SqliteType.Text);
                    var pAlbum = cmd.Parameters.Add("@album", SqliteType.Text);
                    var pGenero = cmd.Parameters.Add("@genero", SqliteType.Text);
                    var pDuracao = cmd.Parameters.Add("@duracao", SqliteType.Integer);
                    var pDataMod = cmd.Parameters.Add("@dataMod", SqliteType.Integer);
                    var pImagem = cmd.Parameters.Add("@imagem", SqliteType.Blob);
                    var pGosto = cmd.Parameters.Add("@gosto", SqliteType.Integer);

                    foreach (var (musica, dataMod) in musicas)
                    {
                        pCaminho.Value = musica.Caminho;
                        pTitulo.Value = musica.Titulo;
                        pArtista.Value = musica.Artista;
                        pAlbum.Value = musica.Album;
                        pGenero.Value = string.IsNullOrEmpty(musica.Genero) ? (object)DBNull.Value : musica.Genero;
                        pDuracao.Value = (int)musica.Duracao.TotalSeconds;
                        pDataMod.Value = dataMod;
                        pImagem.Value = musica.Image.Length > 0 ? musica.Image : (object)DBNull.Value;
                        pGosto.Value = musica.Gosto ? 1 : 0;
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
            }
        }

        public void AtualizarMetadados(string caminho, string titulo, string artista, string album)
        {
            using (SqliteConnection connection = new(_database))
            {
                connection.Open();

                SqliteCommand cmd = connection.CreateCommand();
                cmd.CommandText = @"
            UPDATE Musicas
            SET Titulo = @titulo, Artista = @artista, Album = @album
            WHERE Caminho = @caminho";

                cmd.Parameters.AddWithValue("@titulo", titulo);
                cmd.Parameters.AddWithValue("@artista", artista);
                cmd.Parameters.AddWithValue("@album", album);
                cmd.Parameters.AddWithValue("@caminho", caminho);

                cmd.ExecuteNonQuery();
            }
        }
    }
}