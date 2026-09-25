using Microsoft.Extensions.DependencyInjection;
using Project_Kitsune.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FileIO = Microsoft.VisualBasic.FileIO;

namespace Project_Kitsune.Services
{
    public class BibliotecaService
    {
        public DatabaseService databaseService { get; set; } = App.ServiceProvider.GetRequiredService<DatabaseService>();

        protected HashSet<string> ExtensoesValidas = new(StringComparer.OrdinalIgnoreCase)
            {
                ".mp3", ".flac", ".wav", ".ogg", ".m4a", ".aac", ".wma", ".amr", ".opus", ".midi", ".ape", ".aiff"
            };

        private readonly List<FileSystemWatcher> _watchers = new();
        public ObservableCollection<Music> Musicas { get; } = new();
        private readonly ConfiguracaoService _configuracaoService = App.ServiceProvider.GetRequiredService<ConfiguracaoService>();
        private readonly OrdenacaoService _ordenacaoService = App.ServiceProvider.GetRequiredService<OrdenacaoService>();
        private bool _inicializado = false;

        public event Action? BibliotecaCarregada;

        public void LerMusicasDaPasta(string caminhoPasta, Action<List<Music>> aoCompletarLote)
        {
            if (!Directory.Exists(caminhoPasta)) return;

            List<string> arquivos;
            try
            {
                arquivos = Directory.EnumerateFiles(caminhoPasta, "*.*", SearchOption.TopDirectoryOnly)
                                     .Where(f => ExtensoesValidas.Contains(Path.GetExtension(f)))
                                     .ToList();
            }
            catch (IOException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dispositivo indisponível ao ler '{caminhoPasta}': {ex.Message}");
                return;
            }
            catch (UnauthorizedAccessException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sem permissão ao ler '{caminhoPasta}': {ex.Message}");
                return;
            }

            var cacheCompleto = databaseService.ObterTodoCache();
            var loteUi = new System.Collections.Concurrent.ConcurrentQueue<Music>();
            var paraSalvar = new System.Collections.Concurrent.ConcurrentBag<(Music Musica, long DataMod)>();
            int processados = 0;

            ParallelOptions opcoes = new ParallelOptions()
            {
                MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, 6)
            };

            Parallel.ForEach(arquivos, opcoes, item =>
            {
                try
                {
                    FileInfo fileInfo = new(item);
                    long dataModificacaoAtual = fileInfo.LastWriteTimeUtc.Ticks;

                    bool temCache = cacheCompleto.TryGetValue(item, out var entradaCache);
                    Music? musicaCache = temCache ? entradaCache.Musica : null;
                    long dataModificacaoCache = temCache ? entradaCache.DataModificacao : 0;
                    Music music;

                    if (musicaCache != null && dataModificacaoCache == dataModificacaoAtual)
                    {
                        musicaCache.DataAdicionado = fileInfo.CreationTimeUtc;

                        string caminhoLrc = Path.Combine(caminhoPasta, Path.GetFileNameWithoutExtension(item) + ".kc.lrc");
                        musicaCache.TemLetraDisponivel = File.Exists(caminhoLrc) || musicaCache.TemLetraDisponivel;

                        music = musicaCache;
                    }
                    else
                    {
                        using (TagLib.File tagFile = TagLib.File.Create(item))
                        {
                            byte[] imagem = Array.Empty<byte>();
                            if (tagFile.Tag.Pictures.Length > 0)
                                imagem = tagFile.Tag.Pictures[0].Data.Data;
                            string caminhoLrc = Path.Combine(caminhoPasta, Path.GetFileNameWithoutExtension(item) + ".kc.lrc");

                            music = new Music
                            {
                                Titulo = tagFile.Tag.Title ?? Path.GetFileNameWithoutExtension(item),
                                Artista = tagFile.Tag.FirstPerformer ?? "Unknown",
                                Album = tagFile.Tag.Album ?? "Unknown",
                                Genero = tagFile.Tag.FirstGenre ?? "Unknown",
                                Caminho = item,
                                Duracao = tagFile.Properties.Duration,
                                DuracaoArredondada = TimeSpan.FromSeconds((int)tagFile.Properties.Duration.TotalSeconds),
                                DataAdicionado = fileInfo.CreationTimeUtc,
                                VezesTocada = databaseService.ObterVezesTocada(item),
                                Gosto = musicaCache?.Gosto ?? false,
                                Image = imagem,
                                TemLetraDisponivel = File.Exists(caminhoLrc) || !string.IsNullOrWhiteSpace(tagFile.Tag.Lyrics)
                            };
                        }

                        paraSalvar.Add((music, dataModificacaoAtual));
                    }

                    loteUi.Enqueue(music);
                    int total = Interlocked.Increment(ref processados);

                    if (total % 8 == 0)
                    {
                        List<Music> lote = new List<Music>();
                        while (lote.Count < 8 && loteUi.TryDequeue(out var m)) lote.Add(m);
                        if (lote.Count > 0) aoCompletarLote(lote);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ERRO: {ex.Message}");
                }
            });

            List<Music> restante = new List<Music>();
            while (loteUi.TryDequeue(out var m)) restante.Add(m);
            if (restante.Count > 0) aoCompletarLote(restante);

            if (!paraSalvar.IsEmpty)
            {
                databaseService.SalvarMusicasEmLote(paraSalvar.ToList());
            }
        }

        public Music? LerMusicaPorCaminho(string caminho)
        {
            try
            {
                FileInfo fileInfo = new(caminho);

                using (TagLib.File tagFile = TagLib.File.Create(caminho))
                {
                    byte[] imagem = Array.Empty<byte>();

                    if (tagFile.Tag.Pictures.Length > 0)
                    {
                        imagem = tagFile.Tag.Pictures[0].Data.Data;
                    }

                    return new Music
                    {
                        Titulo = tagFile.Tag.Title ?? Path.GetFileNameWithoutExtension(caminho),
                        Artista = tagFile.Tag.FirstPerformer ?? "Unknown",
                        Album = tagFile.Tag.Album ?? "Unknown",
                        Genero = tagFile.Tag.FirstGenre ?? "Unknown",
                        Caminho = caminho,
                        Duracao = tagFile.Properties.Duration,
                        DuracaoArredondada = TimeSpan.FromSeconds((int)tagFile.Properties.Duration.TotalSeconds),
                        DataAdicionado = fileInfo.CreationTimeUtc,
                        VezesTocada = databaseService.ObterVezesTocada(caminho),
                        Gosto = databaseService.ObterMusicaCache(caminho).Musica?.Gosto ?? false,
                        Image = imagem
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao ler o caminho: {ex.Message}");
                return null;
            }
        }

        public async Task<Letra?> ObterLetra(Music musica)
        {
            string pasta = Path.GetDirectoryName(musica.Caminho)!;
            string nomeBase = Path.GetFileNameWithoutExtension(musica.Caminho);
            string caminhoLrcKitsune = Path.Combine(pasta, nomeBase + ".kc.lrc");
            string caminhoLrcGenerico = Path.Combine(pasta, nomeBase + ".lrc");

            // 1. .kc.lrc local tem prioridade (edição manual do utilizador)
            if (File.Exists(caminhoLrcKitsune))
            {
                try
                {
                    string conteudo = await File.ReadAllTextAsync(caminhoLrcKitsune);
                    if (!string.IsNullOrEmpty(conteudo))
                    {
                        musica.TemLetraDisponivel = true;
                        return ParsearLetra(conteudo, FonteLetra.ArquivoLrc, caminhoLrcKitsune);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ERRO ao ler .lrc: {ex.Message}");
                }
            }

            if (File.Exists(caminhoLrcGenerico))
            {
                try
                {
                    string conteudo = await File.ReadAllTextAsync(caminhoLrcKitsune);
                    if (!string.IsNullOrEmpty(conteudo))
                    {
                        musica.TemLetraDisponivel = true;
                        return ParsearLetra(conteudo, FonteLetra.ArquivoLrc, caminhoLrcKitsune);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ERRO ao ler .kc.lrc: {ex.Message}");
                }
            }

            // 2. busca na API LRCLIB
            try
            {
                string? letraLrclib = await BuscarLetraLrclib(musica.Titulo, musica.Artista, musica.Album, musica.Duracao);
                if (!string.IsNullOrWhiteSpace(letraLrclib))
                {
                    await File.WriteAllTextAsync(caminhoLrcKitsune, letraLrclib);
                    musica.TemLetraDisponivel = true;
                    return ParsearLetra(letraLrclib, FonteLetra.Lrclib, caminhoLrcKitsune);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERRO ao buscar letra na LRCLIB: {ex.Message}");
            }

            // 3. letra embutida na tag do ficheiro de áudio
            Letra? letraEmbutida = await Task.Run(() =>
            {
                try
                {
                    using (TagLib.File tagFile = TagLib.File.Create(musica.Caminho))
                    {
                        string? letra = tagFile.Tag.Lyrics;
                        if (!string.IsNullOrWhiteSpace(letra))
                        {
                            musica.TemLetraDisponivel = true;
                            return ParsearLetra(letra, FonteLetra.Embutida, musica.Caminho);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ERRO ao ler letra embutida: {ex.Message}");
                }
                return null;
            });

            if (letraEmbutida != null) return letraEmbutida;

            return null;
        }

        private static readonly Regex RegexTimestamp = new(@"\[(\d{1,2}):(\d{2})(?:[.:](\d{1,3}))?\]", RegexOptions.Compiled);

        private List<LinhaLetra> ExtrairLinhasComTempo(string linha)
        {
            var resultado = new List<LinhaLetra>();
            var matches = RegexTimestamp.Matches(linha);
            if (matches.Count == 0) return resultado;

            // texto é tudo depois do ÚLTIMO colchete de timestamp
            var ultimoMatch = matches[^1];
            string texto = linha.Substring(ultimoMatch.Index + ultimoMatch.Length).Trim();

            foreach (Match m in matches)
            {
                int minutos = int.Parse(m.Groups[1].Value);
                int segundos = int.Parse(m.Groups[2].Value);
                int centesimos = 0;
                if (m.Groups[3].Success)
                {
                    string frac = m.Groups[3].Value.PadRight(3, '0').Substring(0, 3);
                    centesimos = int.Parse(frac);
                }
                TimeSpan tempo = new(0, 0, minutos, segundos, centesimos);
                resultado.Add(new LinhaLetra { Tempo = tempo, Texto = texto });
            }
            return resultado;
        }

        private Letra ParsearLetra(string conteudo, FonteLetra fonte, string? caminhoOrigem)
        {
            Letra letra = new Letra()
            {
                ConteudoBruto = conteudo,
                Fonte = fonte,
                CaminhoOrigem = caminhoOrigem
            };

            string conteudoNormalizado = conteudo.Replace("\r\n", "\n").Replace("\r", "\n");
            string[] linhas = conteudoNormalizado.Split('\n');

            foreach (string linhaBruta in linhas)
            {
                string linha = linhaBruta.Trim();
                if (linha.Length == 0) continue;

                var extraidas = ExtrairLinhasComTempo(linha);
                if (extraidas.Count > 0)
                    letra.Linhas.AddRange(extraidas);
            }

            if (letra.Linhas.Count > 0)
            {
                letra.TemTimestampsReais = true; // encontrou pelo menos uma linha com timestamp válido
            }
            else
            {
                // Fallback: texto plano, sem timestamps
                foreach (string linhaBruta in linhas)
                {
                    string texto = linhaBruta.Trim();
                    if (texto.Length > 0)
                    {
                        letra.Linhas.Add(new LinhaLetra { Tempo = TimeSpan.Zero, Texto = texto });
                    }
                }
                letra.TemTimestampsReais = false;
            }

            letra.Linhas = letra.Linhas.OrderBy(l => l.Tempo).ToList();
            return letra;
        }

        private static readonly HttpClient _httpClient = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        private async Task<string?> BuscarLetraLrclib(string titulo, string artista, string album, TimeSpan duracao)
        {
            string url = $"https://lrclib.net/api/get?track_name={Uri.EscapeDataString(titulo)}" +
                         $"&artist_name={Uri.EscapeDataString(artista)}" +
                         $"&album_name={Uri.EscapeDataString(album)}" +
                         $"&duration={(int)duracao.TotalSeconds}";

            try
            {
                HttpResponseMessage resposta = await _httpClient.GetAsync(url);
                if (!resposta.IsSuccessStatusCode) return null;

                string json = await resposta.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("syncedLyrics", out JsonElement syncedEl) &&
                    syncedEl.ValueKind == JsonValueKind.String)
                {
                    return syncedEl.GetString();
                }

                if (doc.RootElement.TryGetProperty("plainLyrics", out JsonElement plainEl) &&
                    plainEl.ValueKind == JsonValueKind.String)
                {
                    return plainEl.GetString();
                }
            }
            catch (TaskCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("LRCLIB: tempo limite excedido");
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"LRCLIB: erro de rede - {ex.Message}");
            }

            return null;
        }

        public async Task CarregarBibliotecaAsync(IEnumerable<string> pastas, string criterio, bool descendente)
        {
            var todasMusicas = new List<Music>();
            object lockMusicas = new();

            var tarefas = pastas
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(pasta => Task.Run(() =>
                {
                    LerMusicasDaPasta(pasta, lote =>
                    {
                        lock (lockMusicas) { todasMusicas.AddRange(lote); }
                    });
                }))
                .ToList();

            await Task.WhenAll(tarefas);

            List<Music> ordenadas = _ordenacaoService.Ordenar(todasMusicas, criterio, descendente);

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                AtualizarMusicas(ordenadas);
            });
            BibliotecaCarregada?.Invoke();
        }

        private void AtualizarMusicas(List<Music> novasOrdenadas)
        {
            for (int i = Musicas.Count - 1; i >= 0; i--)
            {
                if (!novasOrdenadas.Any(m => m.Caminho == Musicas[i].Caminho))
                    Musicas.RemoveAt(i);
            }

            foreach (var m in novasOrdenadas)
            {
                if (!Musicas.Any(x => x.Caminho == m.Caminho))
                    Musicas.Add(m);
            }

            for (int novoIndice = 0; novoIndice < novasOrdenadas.Count; novoIndice++)
            {
                string caminho = novasOrdenadas[novoIndice].Caminho;
                int indiceAtual = -1;
                for (int i = 0; i < Musicas.Count; i++)
                {
                    if (Musicas[i].Caminho == caminho) { indiceAtual = i; break; }
                }
                if (indiceAtual != -1 && indiceAtual != novoIndice)
                    Musicas.Move(indiceAtual, novoIndice);
            }
        }

        public void IniciarMonitorizacao(IEnumerable<string> pastas)
        {
            PararMonitorizacao();

            foreach (string pasta in pastas.Where(p => !string.IsNullOrWhiteSpace(p) && Directory.Exists(p)))
            {
                var watcher = new FileSystemWatcher(pasta)
                {
                    EnableRaisingEvents = true,
                    NotifyFilter = NotifyFilters.FileName
                };

                watcher.Created += Watcher_Created;
                watcher.Deleted += Watcher_Deleted;
                watcher.Renamed += Watcher_Renamed;
                watcher.Error += Watcher_Error;

                _watchers.Add(watcher);
            }
        }

        public void PararMonitorizacao()
        {
            foreach (var watcher in _watchers)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            _watchers.Clear();
        }

        private void ReordenarMusicas()
        {
            Configuracao config = _configuracaoService.CarregarConfiguracao();
            List<Music> ordenadas = _ordenacaoService.Ordenar(Musicas.ToList(), config.OrdemMusicList, config.OrdemDescendente);
            AtualizarMusicas(ordenadas);
        }

        private async void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            if (!ExtensoesValidas.Contains(Path.GetExtension(e.FullPath))) return;

            bool disponivel = await AguardarArquivoLivreAsync(e.FullPath);
            if (!disponivel) return;

            Music? musica = LerMusicaPorCaminho(e.FullPath);
            if (musica == null) return;

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                bool jaExiste = Musicas.Any(m => string.Equals(m.Caminho, musica.Caminho, StringComparison.OrdinalIgnoreCase));
                if (!jaExiste)
                {
                    Musicas.Add(musica);
                    ReordenarMusicas();
                }
            });
        }

        private async void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Music? musicaAntiga = Musicas.FirstOrDefault(m =>
                    string.Equals(m.Caminho, e.OldFullPath, StringComparison.OrdinalIgnoreCase));
                if (musicaAntiga != null)
                    Musicas.Remove(musicaAntiga);
            });

            string extensao = Path.GetExtension(e.FullPath).ToLowerInvariant();
            if (!ExtensoesValidas.Select(ext => ext.ToLowerInvariant()).Contains(extensao)) return;

            bool disponivel = await AguardarArquivoLivreAsync(e.FullPath);
            if (!disponivel) return;

            Music? musicaNova = LerMusicaPorCaminho(e.FullPath);
            if (musicaNova == null) return;

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                bool jaExiste = Musicas.Any(m => string.Equals(m.Caminho, musicaNova.Caminho, StringComparison.OrdinalIgnoreCase));
                if (!jaExiste)
                {
                    Musicas.Add(musicaNova);
                    ReordenarMusicas();
                }
            });
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Music? musica = Musicas.FirstOrDefault(m =>
                    string.Equals(m.Caminho, e.FullPath, StringComparison.OrdinalIgnoreCase));

                if (musica != null) Musicas.Remove(musica);
                ReordenarMusicas();
            });
        }

        private void Watcher_Error(object sender, ErrorEventArgs e)
        {
            if (sender is FileSystemWatcher watcher)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                _watchers.Remove(watcher);
            }
        }

        private async Task<bool> AguardarArquivoLivreAsync(string caminho)
        {
            for (int tentativa = 0; tentativa < 10; tentativa++)
            {
                try
                {
                    using FileStream stream = File.Open(caminho, FileMode.Open, FileAccess.Read, FileShare.None);
                    return true; // conseguiu abrir em exclusivo = ficheiro livre
                }
                catch (IOException)
                {
                    await Task.Delay(300);
                }
            }
            return false;
        }

        public async Task GarantirBibliotecaCarregadaAsync(IEnumerable<string> pastas, bool forcar = false)
        {
            if (_inicializado && !forcar) return;
            _inicializado = true;

            Configuracao config = _configuracaoService.CarregarConfiguracao();
            await CarregarBibliotecaAsync(pastas, config.OrdemMusicList, config.OrdemDescendente);
            IniciarMonitorizacao(pastas);
        }

        public void EliminarMusica(Music musica)
        {
            MoverParaLixeira(musica.Caminho);
        }

        public void MoverParaLixeira(string caminho)
        {
            if (File.Exists(caminho))
            {
                FileIO.FileSystem.DeleteFile(caminho, FileIO.UIOption.OnlyErrorDialogs, FileIO.RecycleOption.SendToRecycleBin);
            }
        }

        public void SalvarEdicaoMusica(Music musica, string novoTitulo, string novoArtista, string novoAlbum)
        {
            // 1. Ficheiro físico
            var tagFile = TagLib.File.Create(musica.Caminho);
            tagFile.Tag.Title = novoTitulo;
            tagFile.Tag.Performers = new[] { novoArtista };
            tagFile.Tag.Album = novoAlbum;
            tagFile.Save();

            // 2. Base de dados
            databaseService.AtualizarMetadados(musica.Caminho, novoTitulo, novoArtista, novoAlbum);

            // 3. Objeto em memória (pra UI atualizar sem precisar recarregar tudo)
            musica.Titulo = novoTitulo;
            musica.Artista = novoArtista;
            musica.Album = novoAlbum;
        }
    }
}