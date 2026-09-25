using CommunityToolkit.Mvvm.Input;
using LibVLCSharp.Shared;
using Microsoft.Extensions.DependencyInjection;
using Project_Kitsune.Models;
using Project_Kitsune.Services;
using Project_Kitsune.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace Project_Kitsune.ViewModels
{
    public partial class PlayerViewModel : INotifyPropertyChanged, IDisposable
    {
        /* ESTADOS - (Campos/Propriedades) */
        public ObservableCollection<Music> Musicas => BibliotecaService.Musicas;
        public ObservableCollection<BandaEqualizador> Bandas { get; set; } = new ObservableCollection<BandaEqualizador>();
        public DatabaseService DatabaseService { get; set; }
        public BibliotecaService BibliotecaService { get; set; }
        protected ConfiguracaoService configuracaoService;
        protected AudioPlayerService audioPlayerService;
        private DispatcherTimer _timer;
        private List<Music>? _listaEmbaralhada = null;
        public ObservableCollection<PresetEqualizador> Presets { get; set; } = new ObservableCollection<PresetEqualizador>();
        public ObservableCollection<Playlist> PlaylistsDisponiveis { get; } = new();
        private uint _presetSelecionadoIndice;

        public uint PresetSelecionadoIndice
        {
            get => _presetSelecionadoIndice;
            set
            {
                _presetSelecionadoIndice = value;
                OnPropertyChanged();

                if (value == uint.MaxValue) return;

                audioPlayerService.DefinirEqualizador((int)value);
                AtualizarBandasComPresetAtual();

                var config = configuracaoService.CarregarConfiguracao();
                config.EqualizadorPreset = (int)value;
                config.EqualizadorGanhosCustom = null;
                configuracaoService.GuardarConfiguracao(config);
            }
        }

        private int _indiceEmbaralhado = -1;

        private double _progressoPercentual;

        public double ProgressoPercentual
        {
            get => _progressoPercentual;
            set
            {
                _progressoPercentual = value;
                OnPropertyChanged();
            }
        }

        private double _posicaoAtualMs;

        public double PosicaoAtualMs
        {
            get => _posicaoAtualMs;
            set
            {
                _posicaoAtualMs = value;
                OnPropertyChanged();
            }
        }

        private bool _estaArrastando;

        public bool EstaArrastando
        {
            get => _estaArrastando;
            set
            {
                _estaArrastando = value;
                OnPropertyChanged();
            }
        }

        private double _duracaoTotalMs = 100;

        public double DuracaoTotalMs
        {
            get => _duracaoTotalMs;
            set
            {
                _duracaoTotalMs = value;
                OnPropertyChanged();
            }
        }

        private long _posicaoRestaurar = 0;
        private long _duracaoRestaurar = 0;
        private bool _temPosicaoRestaurada = false;

        private string _tempoAtualFormatado = "00:00";

        public string TempoAtualFormatado
        {
            get => _tempoAtualFormatado;
            set
            {
                _tempoAtualFormatado = value;
                OnPropertyChanged();
            }
        }

        private string _tempoTotalFormatado = "00:00";

        public string TempoTotalFormatado
        {
            get => _tempoTotalFormatado;
            set
            {
                _tempoTotalFormatado = value;
                OnPropertyChanged();
            }
        }

        private Music? _musicaAtual;

        public Music? MusicaAtual
        {
            get => _musicaAtual;
            set
            {
                _musicaAtual = value;
                OnPropertyChanged();
            }
        }

        private bool _estaTocando;

        public bool EstaTocando
        {
            get => _estaTocando;
            set
            {
                _estaTocando = value;
                OnPropertyChanged();
            }
        }

        private bool _gostoAtual;

        public bool GostoAtual
        {
            get => _gostoAtual;
            set
            {
                _gostoAtual = value;
                OnPropertyChanged();
            }
        }

        public enum ModoRepeticao
        {
            Nenhum,
            Aleatorio,
            RepetirUma,
            RepetirLista
        }

        private ModoRepeticao _repeticao = ModoRepeticao.Nenhum;

        public ModoRepeticao Repeticao
        {
            get => _repeticao;
            set
            {
                _repeticao = value;
                OnPropertyChanged();
            }
        }

        protected Random _random = new Random();
        public List<Music> ListaAtual { get; set; } = new List<Music>();

        public bool TemLetra => LetraAtual?.Linhas.Count > 0;

        private bool _mostrarletra;

        public bool MostrarLetra
        {
            get => _mostrarletra;
            set
            {
                _mostrarletra = value;
                OnPropertyChanged();
            }
        }

        private Letra? _letraAtual;

        public Letra? LetraAtual
        {
            get => _letraAtual;
            set
            {
                if (_letraAtual == value)
                    return;

                _letraAtual = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(TemLetra));
            }
        }

        private bool _scrollAutomaticoAtivo = false;

        public bool ScrollAutomaticoAtivo
        {
            get => _scrollAutomaticoAtivo;
            set
            {
                _scrollAutomaticoAtivo = value;
                OnPropertyChanged();
            }
        }

        private LinhaLetra? _linhaAtual;

        public LinhaLetra? LinhaAtual
        {
            get => _linhaAtual ?? (LetraAtual?.Sincronizada == false ? LetraAtual.Linhas?.FirstOrDefault() : null);
            set
            {
                _linhaAtual = value;
                OnPropertyChanged();
            }
        }

        private bool _mostrarEqualizador;

        public bool MostrarEqualizador
        {
            get => _mostrarEqualizador;
            set
            {
                _mostrarEqualizador = value;
                OnPropertyChanged();
            }
        }

        public bool _oculatarControles = false;

        public bool OcultarControles
        {
            get => _oculatarControles;
            set
            {
                _oculatarControles = value;
                OnPropertyChanged();
            }
        }

        private bool _carregando = false;
        private CancellationTokenSource? _letraCts;

        private bool mostrarConfirmacaoExclusao;

        public bool MostrarConfirmacaoExclusao
        {
            get => mostrarConfirmacaoExclusao;
            set
            {
                mostrarConfirmacaoExclusao = value;
                OnPropertyChanged();
            }
        }

        private Music MusicaParaExcluir = null!;

        public PlayerViewModel(AudioPlayerService _audioPlayerService)
        {
            audioPlayerService = _audioPlayerService;
            configuracaoService = App.ServiceProvider.GetRequiredService<ConfiguracaoService>();
            DatabaseService = App.ServiceProvider.GetRequiredService<DatabaseService>();
            BibliotecaService = App.ServiceProvider.GetRequiredService<BibliotecaService>();

            Configuracao config = configuracaoService.CarregarConfiguracao();
            if (config.ModoRepeticao == "RepetirUma") Repeticao = ModoRepeticao.RepetirUma;
            if (config.ModoRepeticao == "RepetirLista") Repeticao = ModoRepeticao.RepetirLista;
            if (config.ModoRepeticao == "Aleatorio")
            {
                Repeticao = ModoRepeticao.Aleatorio;
                _listaEmbaralhada = ListaAtual.ToList();
                Shuffle(_listaEmbaralhada, _random);
            }
            foreach (var preset in audioPlayerService.CarregarPresets()) Presets.Add(preset);

            for (int i = 0; i < 10; i++)
            {
                var banda = new BandaEqualizador
                {
                    Indice = (uint)i,
                    Frequencia = (float)audioPlayerService._frequenciasBanda[i], // precisa existir/expor isto
                    Ganho = 0f
                };
                banda.OnGanhoAlterado += (idx, valor) =>
                {
                    audioPlayerService.DefinirGanhoBanda(idx, valor);
                    var cfg = configuracaoService.CarregarConfiguracao();
                    cfg.EqualizadorGanhosCustom ??= new float[10];
                    cfg.EqualizadorGanhosCustom[idx] = valor;
                    cfg.EqualizadorPreset = -1; // deixou de corresponder a um preset
                    configuracaoService.GuardarConfiguracao(cfg);
                };
                Bandas.Add(banda);
            }

            if (config.EqualizadorGanhosCustom != null)
            {
                for (int i = 0; i < config.EqualizadorGanhosCustom.Length && i < Bandas.Count; i++)
                    Bandas[i].AtualizarGanhoSemNotificar(config.EqualizadorGanhosCustom[i]);
            }
            else if (config.EqualizadorPreset >= 0 && config.EqualizadorPreset < Presets.Count)
            {
                _presetSelecionadoIndice = (uint)config.EqualizadorPreset; // sem passar pelo setter, para não regravar config nem chamar DefinirEqualizador de novo (AudioPlayerService já aplicou isso no construtor dele)
                AtualizarBandasComPresetAtual();
            }

            audioPlayerService.MusicaTerminou += AudioPlayer_MusicaTerminou;
            audioPlayerService.ErroReproducao += AudioPlayer_ErroReproducao;
            audioPlayerService.PlaybackStarted += AudioPlayer_PlaybackStarted;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };

            _timer.Tick += Timer_Tick;
            _timer.Start();
            ListaAtual = Musicas.ToList();
            RestaurarUltimaMusica();
        }

        /* METODOS */

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Tick -= Timer_Tick;
                _timer.Stop();
                _timer = null!;
            }

            if (audioPlayerService != null)
            {
                audioPlayerService.MusicaTerminou -= AudioPlayer_MusicaTerminou;
                audioPlayerService.ErroReproducao -= AudioPlayer_ErroReproducao;
                audioPlayerService.PlaybackStarted -= AudioPlayer_PlaybackStarted;
            }
        }

        private void AudioPlayer_MusicaTerminou()
        {
            InvokeOnUI(() =>
            {
                var musicaQueTerminou = MusicaAtual;

                TocarProxima();

                if (musicaQueTerminou != null)
                {
                    musicaQueTerminou.VezesTocada++;
                    DatabaseService.IncrementarReproducoes(musicaQueTerminou.Caminho);
                }
            });
        }

        private void AudioPlayer_ErroReproducao()
        {
            InvokeOnUI(() =>
            {
                EstaTocando = false;
                MusicaAtual = null;
                DefinirCarregando(false);
            });
        }

        private void AudioPlayer_PlaybackStarted()
        {
            InvokeOnUI(() =>
            {
                EstaTocando = true;
                DefinirCarregando(false);
            });
            _ = CarregarLetraAtual();
            _ = AplicarPosicaoRestauradaQuandoProntoAsync();
        }

        private async Task AplicarPosicaoRestauradaQuandoProntoAsync()
        {
            try
            {
                const int intervaloMs = 100;
                const int timeoutMs = 3000;
                int waited = 0;

                while (waited < timeoutMs)
                {
                    long dur = 0;
                    try { dur = audioPlayerService.ObterDuracaoTotal(); } catch { dur = 0; }

                    if (dur > 0) break;

                    await Task.Delay(intervaloMs).ConfigureAwait(false);
                    waited += intervaloMs;
                }

                if (_temPosicaoRestaurada && _posicaoRestaurar > 0)
                {
                    // limita a posição para não exceder a duração conhecida
                    long duracao = 0;
                    try { duracao = audioPlayerService.ObterDuracaoTotal(); } catch { duracao = 0; }

                    long posParaAplicar = _posicaoRestaurar;
                    if (duracao > 0 && posParaAplicar >= duracao)
                    {
                        posParaAplicar = Math.Max(0, duracao - 1000); // ajusta 1s antes do fim
                    }

                    // aplicar no thread da UI
                    InvokeOnUI(() =>
                    {
                        try
                        {
                            audioPlayerService.RetomarAudio();
                            DefinirPosicaoManual(posParaAplicar);

                            long pos = Math.Max(0, audioPlayerService.ObterPosicaoAtual());
                            long dur = Math.Max(0, audioPlayerService.ObterDuracaoTotal());

                            PosicaoAtualMs = pos;
                            DuracaoTotalMs = dur;
                            TempoAtualFormatado = FormatarTempo(pos);
                            TempoTotalFormatado = dur > 0 ? FormatarTempo(dur) : "00:00";
                            ProgressoPercentual = (dur > 0) ? (double)pos / dur * 100 : 0;
                        }
                        catch { }
                    });

                    // limpa flags
                    _posicaoRestaurar = 0;
                    _temPosicaoRestaurada = false;
                }

                InvokeOnUI(() =>
                {
                    if (MusicaAtual != null)
                    {
                        try
                        {
                            var cfg = configuracaoService.CarregarConfiguracao();
                            cfg.UltimaMusica = new UltimaMusicaInfo
                            {
                                Caminho = MusicaAtual?.Caminho,
                                PosicaoMs = audioPlayerService.ObterPosicaoAtual(),
                                DuracaoMS = audioPlayerService.ObterDuracaoTotal(),
                                EstavaTocando = true,
                                SalvoEm = DateTime.UtcNow
                            };
                            configuracaoService.GuardarConfiguracao(cfg);
                        }
                        catch { }
                    }
                });
            }
            catch { }
        }

        public void RestaurarUltimaMusica()
        {
            if (MusicaAtual != null) return;
            try
            {
                var cfg = configuracaoService.CarregarConfiguracao();
                var last = cfg.UltimaMusica;
                if (last?.Caminho != null && File.Exists(last.Caminho))
                {
                    var encontrada = Musicas.FirstOrDefault(m => string.Equals(m.Caminho, last.Caminho, StringComparison.OrdinalIgnoreCase));
                    if (encontrada != null)
                    {
                        MusicaAtual = encontrada;

                        if (last.PosicaoMs > 0)
                        {
                            _posicaoRestaurar = last.PosicaoMs;
                            _duracaoRestaurar = (long)encontrada.Duracao.TotalMilliseconds;
                            _temPosicaoRestaurada = true;
                            PosicaoAtualMs = _posicaoRestaurar;
                            TempoAtualFormatado = FormatarTempo(_posicaoRestaurar);
                            TempoTotalFormatado = FormatarTempo(_duracaoRestaurar);
                            DuracaoTotalMs = _duracaoRestaurar;

                            if (_duracaoRestaurar > 0)
                            {
                                ProgressoPercentual = (double)_posicaoRestaurar / _duracaoRestaurar * 100;
                            }
                        }
                        EstaTocando = false; // ou last.EstavaTocando, se decidires retomar o play
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao restaurar última música: {ex.Message}");
            }
        }

        private void InvokeOnUI(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            if (dispatcher.CheckAccess())
                action();
            else
                dispatcher.BeginInvoke(action);
        }

        public void TocarMusica(Music musica, List<Music>? lista = null, bool manterIndiceEmbaralhado = false)
        {
            if (_carregando) return;

            if (lista != null)
            {
                ListaAtual = lista;
            }

            while (!File.Exists(musica.Caminho))
            {
                int indiceRemovido = ListaAtual.FindIndex(m => string.Equals(m.Caminho, musica.Caminho, StringComparison.OrdinalIgnoreCase));
                Musicas.Remove(musica);
                ListaAtual.RemoveAll(m => string.Equals(m.Caminho, musica.Caminho, StringComparison.OrdinalIgnoreCase));

                if (ListaAtual.Count == 0)
                {
                    _letraCts?.Cancel();
                    MostrarLetra = false;
                    LetraAtual = null;
                    EstaTocando = false;
                    MusicaAtual = null;
                    return;
                }

                int proximoIndice = (indiceRemovido >= 0 && indiceRemovido < ListaAtual.Count) ? indiceRemovido : 0;
                musica = ListaAtual[proximoIndice];
            }
            if (!string.Equals(MusicaAtual?.Caminho, musica.Caminho, StringComparison.OrdinalIgnoreCase))
            {
                _posicaoRestaurar = 0;
                _temPosicaoRestaurada = false;
            }
            try
            {
                DefinirCarregando(true);

                if (lista != null)
                {
                    ListaAtual = lista;

                    if (Repeticao == ModoRepeticao.Aleatorio)
                    {
                        _listaEmbaralhada = ListaAtual.ToList();
                        Shuffle(_listaEmbaralhada, _random);
                    }
                }

                MusicaAtual = musica;
                GostoAtual = musica.Gosto;

                if (_listaEmbaralhada != null && !manterIndiceEmbaralhado)
                {
                    _indiceEmbaralhado = _listaEmbaralhada.FindIndex(m =>
                        !string.IsNullOrEmpty(m?.Caminho)
                        && string.Equals(m.Caminho, musica.Caminho, StringComparison.OrdinalIgnoreCase));
                    if (_indiceEmbaralhado < 0) _indiceEmbaralhado = 0;
                }

                DuracaoTotalMs = musica.Duracao.TotalMilliseconds;
                PosicaoAtualMs = 0;
                ProgressoPercentual = 0;
                TempoTotalFormatado = FormatarTempo((long)DuracaoTotalMs);
                TempoAtualFormatado = "0:00";

                try
                {
                    audioPlayerService.TocarAudio(MusicaAtual.Caminho);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    EstaTocando = false;
                    DefinirCarregando(false); // ✅ NOVO: Garante que não fica travado
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                DefinirCarregando(false); // ✅ NOVO: Garante que não fica travado
            }
        }

        private void TocarProxima()
        {
            try
            {
                if (MusicaAtual == null || ListaAtual.Count == 0) return;

                //ListaAtual.RemoveAll(m => !File.Exists(m.Caminho));

                if (Repeticao == ModoRepeticao.RepetirUma && audioPlayerService.Terminou == true)
                {
                    TocarMusica(MusicaAtual);
                    return;
                }
                if (Repeticao == ModoRepeticao.Aleatorio && _listaEmbaralhada != null)
                {
                    _indiceEmbaralhado++;
                    if (_indiceEmbaralhado < _listaEmbaralhada.Count)
                    {
                        TocarMusica(_listaEmbaralhada[_indiceEmbaralhado], manterIndiceEmbaralhado: true);
                    }
                    else
                    {
                        _indiceEmbaralhado = 0;
                        TocarMusica(_listaEmbaralhada[_indiceEmbaralhado], manterIndiceEmbaralhado: true);
                    }
                    return;
                }
                int indiceAtual = ObterIndiceAtual();
                if (indiceAtual == -1)
                {
                    EstaTocando = false;
                    return;
                }
                int proximoIndice = indiceAtual + 1;

                if (proximoIndice < ListaAtual.Count)
                {
                    TocarMusica(ListaAtual[proximoIndice], manterIndiceEmbaralhado: true);
                }
                else if (Repeticao == ModoRepeticao.RepetirLista)
                {
                    TocarMusica(ListaAtual[0]);
                }
                else
                {
                    EstaTocando = false;
                }
                return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                EstaTocando = false;
                DefinirCarregando(false); // ✅ NOVO
            }
        }

        private void TocarAnterior()
        {
            try
            {
                if (MusicaAtual == null || ListaAtual.Count == 0) return;

                //ListaAtual.RemoveAll(m => !File.Exists(m.Caminho));

                if (Repeticao == ModoRepeticao.RepetirUma && audioPlayerService.Terminou == true)
                {
                    TocarMusica(MusicaAtual);
                    return;
                }

                if (Repeticao == ModoRepeticao.Aleatorio && _listaEmbaralhada != null)
                {
                    _indiceEmbaralhado--;
                    if (_indiceEmbaralhado >= 0)
                    {
                        TocarMusica(_listaEmbaralhada[_indiceEmbaralhado], manterIndiceEmbaralhado: true);
                    }
                    else
                    {
                        _indiceEmbaralhado = _listaEmbaralhada.Count - 1;
                        TocarMusica(_listaEmbaralhada[_indiceEmbaralhado], manterIndiceEmbaralhado: true);
                    }
                    return;
                }

                int indiceAtual = ObterIndiceAtual();
                int indiceAnterior = indiceAtual - 1;

                if (indiceAnterior >= 0)
                {
                    TocarMusica(ListaAtual[indiceAnterior]);
                }
                else if (Repeticao == ModoRepeticao.RepetirLista)
                {
                    TocarMusica(ListaAtual[ListaAtual.Count - 1]);
                }
                return;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                EstaTocando = false;
                DefinirCarregando(false); // ✅ NOVO
            }
        }

        public void Pausar()
        {
            try
            {
                audioPlayerService.PausarAudio();
                EstaTocando = false;
            }
            catch (Exception ex)
            {
                EstaTocando = false;
                Console.WriteLine(ex.ToString());
            }
        }

        public void Retomar()
        {
            try
            {
                if (MusicaAtual == null || _carregando) return;

                if (audioPlayerService.ObterPosicaoAtual() <= 0 && audioPlayerService.ObterDuracaoTotal() <= 0)
                {
                    TocarMusica(MusicaAtual, ListaAtual);
                    return;
                }

                audioPlayerService.RetomarAudio();
                EstaTocando = true;
            }
            catch (Exception ex)
            {
                EstaTocando = false;
                Console.WriteLine(ex.ToString());
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (!EstaTocando || EstaArrastando) return;

                // Verifica se a música atual ainda pertence à biblioteca
                if (MusicaAtual != null &&
                 !Musicas.Any(m => string.Equals(
                     m.Caminho,
                     MusicaAtual.Caminho,
                     StringComparison.OrdinalIgnoreCase)))
                {
                    audioPlayerService.PararAudio();
                    _letraCts?.Cancel();
                    MostrarLetra = false;
                    MusicaAtual = null;
                    LetraAtual = null;
                    EstaTocando = false;

                    return;
                }

                long posicao = Math.Max(0, audioPlayerService.ObterPosicaoAtual());
                long duracao = Math.Max(0, audioPlayerService.ObterDuracaoTotal());

                PosicaoAtualMs = posicao;
                DuracaoTotalMs = duracao;

                TempoAtualFormatado = FormatarTempo(posicao);
                TempoTotalFormatado = FormatarTempo(duracao);

                if (duracao > 0)
                    ProgressoPercentual = (double)posicao / duracao * 100;
                else
                    ProgressoPercentual = 0;

                AtualizarLinhaAtual(posicao);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                EstaTocando = false;
                EstaArrastando = false;
            }
        }

        private string FormatarTempo(long milissegundos)
        {
            TimeSpan tempo = TimeSpan.FromMilliseconds(milissegundos);
            if (tempo.TotalHours >= 1)
                return string.Format("{0}:{1:D2}:{2:D2}", (int)tempo.TotalHours, tempo.Minutes, tempo.Seconds);
            return string.Format("{0}:{1:D2}", (int)tempo.TotalMinutes, tempo.Seconds);
        }

        private int ObterIndiceAtual()
        {
            if (MusicaAtual == null) return -1;
            return ListaAtual.FindIndex(m =>
                !string.IsNullOrEmpty(m?.Caminho)
                && !string.IsNullOrEmpty(MusicaAtual.Caminho)
                && string.Equals(m.Caminho, MusicaAtual.Caminho, StringComparison.OrdinalIgnoreCase));
        }

        private async Task CarregarLetraAtual()
        {
            if (MusicaAtual == null) return;

            try
            {
                _letraCts?.Cancel();
            }
            catch { }
            var cts = new CancellationTokenSource();
            _letraCts = cts;
            CancellationToken token = cts.Token;

            try
            {
                var letra = await BibliotecaService.ObterLetra(MusicaAtual).ConfigureAwait(false);

                if (token.IsCancellationRequested) return;

                InvokeOnUI(() =>
                {
                    if (token.IsCancellationRequested) return;
                    LetraAtual = letra;
                    LinhaAtual = null;
                    OnPropertyChanged(nameof(TemLetra));
                });
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                if (!token.IsCancellationRequested)
                {
                    InvokeOnUI(() =>
                    {
                        LetraAtual = null;
                        LinhaAtual = null;
                    });
                }
            }
            finally
            {
                if (_letraCts == cts)
                {
                    _letraCts = null;
                }
            }
        }

        public void DefinirPosicaoManual(long milissegundos)
        {
            try
            {
                audioPlayerService.DefinirPosicao(milissegundos);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void AtualizarLinhaAtual(long posicaoMs)
        {
            if (LetraAtual == null || !LetraAtual.Sincronizada)
            {
                LinhaAtual = null;
                return;
            }

            TimeSpan posicaoAtual = TimeSpan.FromMilliseconds(posicaoMs);

            LinhaLetra? linha = LetraAtual.Linhas
                .Where(l => l.Tempo <= posicaoAtual)
                .OrderByDescending(l => l.Tempo)
                .FirstOrDefault();

            if (linha != LinhaAtual)
            {
                LinhaAtual = linha;
            }
        }

        private void Shuffle<T>(IList<T> list, Random rng)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        private bool PodeNavegar() => !_carregando;

        private void DefinirCarregando(bool valor)
        {
            _carregando = valor;
            ProximaCommand.NotifyCanExecuteChanged();
            AnteriorCommand.NotifyCanExecuteChanged();
            AlternarPlayPauseCommand.NotifyCanExecuteChanged();
        }

        private void AtualizarBandasComPresetAtual()
        {
            if (PresetSelecionadoIndice >= Presets.Count) return;

            var preset = Presets[(int)PresetSelecionadoIndice];

            for (int i = 0; i < preset.Bandas.Count && i < Bandas.Count; i++)
            {
                Bandas[i].AtualizarGanhoSemNotificar((float)preset.Bandas[i].GanhoDb);
            }
        }

        public void CarregarPlaylistsDisponiveis()
        {
            PlaylistsDisponiveis.Clear();
            foreach (var playlist in DatabaseService.ListarPlaylists())
            {
                PlaylistsDisponiveis.Add(playlist);
            }
        }

        private void EliminarMusica(Music musica)
        {
            if (MusicaAtual?.Caminho == musica.Caminho)
            {
                audioPlayerService.PararAudio();
                EstaTocando = false;
                MusicaAtual = null;
            }
            BibliotecaService.EliminarMusica(musica); // manda pra lixeira; FileSystemWatcher cuida do resto
        }

        /* RELAYCOMMANDS */

        [RelayCommand(CanExecute = nameof(PodeNavegar))]
        private void AlternarPlayPause()
        {
            if (EstaTocando)
            {
                Pausar();
            }
            else
            {
                Retomar();
            }
        }

        [RelayCommand(CanExecute = nameof(PodeNavegar))]
        private void Proxima() => TocarProxima();

        [RelayCommand(CanExecute = nameof(PodeNavegar))]
        private void Anterior() => TocarAnterior();

        [RelayCommand]
        private void AlternarRepeticao()
        {
            Configuracao config = configuracaoService.CarregarConfiguracao();
            Repeticao = Repeticao switch
            {
                ModoRepeticao.Nenhum => ModoRepeticao.RepetirLista,
                ModoRepeticao.RepetirLista => ModoRepeticao.RepetirUma,
                ModoRepeticao.RepetirUma => ModoRepeticao.Aleatorio,
                ModoRepeticao.Aleatorio => ModoRepeticao.Nenhum,
                _ => ModoRepeticao.Nenhum
            };
            if (Repeticao == ModoRepeticao.Aleatorio)
            {
                _listaEmbaralhada = ListaAtual.ToList();
                Shuffle(_listaEmbaralhada, _random);

                if (MusicaAtual != null)
                {
                    _indiceEmbaralhado = _listaEmbaralhada.FindIndex(m =>
                        !string.IsNullOrEmpty(m?.Caminho)
                        && string.Equals(m.Caminho, MusicaAtual.Caminho, StringComparison.OrdinalIgnoreCase));
                    if (_indiceEmbaralhado < 0) _indiceEmbaralhado = -1;
                }
                else
                {
                    _indiceEmbaralhado = -1;
                }
            }
            else
            {
                _listaEmbaralhada = null;
                _indiceEmbaralhado = -1;
            }
            config.ModoRepeticao = Repeticao.ToString();
            configuracaoService.GuardarConfiguracao(config);
        }

        [RelayCommand]
        public void AlternarLetras()
        {
            MostrarLetra = !MostrarLetra;
        }

        [RelayCommand]
        public void FecharLetra()
        {
            MostrarLetra = false;
        }

        [RelayCommand]
        public void AlternarEqualizador()
        {
            MostrarEqualizador = !MostrarEqualizador;
        }

        [RelayCommand]
        public void FecharEqualizador()
        {
            MostrarEqualizador = false;
        }

        [RelayCommand]
        private void AlternarGosto()
        {
            if (MusicaAtual == null) return;
            bool novoValor = !MusicaAtual.Gosto;
            MusicaAtual.Gosto = novoValor;
            GostoAtual = novoValor;
            DatabaseService.AlternarGosto(MusicaAtual.Caminho, novoValor);
        }

        [RelayCommand]
        private void SaltarParaLinha(LinhaLetra linha)
        {
            if (linha == null || LetraAtual == null || !LetraAtual.Sincronizada) return;

            long milissegundos = (long)linha.Tempo.TotalMilliseconds;
            DefinirPosicaoManual(milissegundos);
            LinhaAtual = linha;
        }

        [RelayCommand]
        public void AlternarControles()
        {
            OcultarControles = !OcultarControles;
        }

        [RelayCommand]
        private void EditarMusica(Music musica)
        {
            var window = new EditarMusicaWindow(musica, BibliotecaService)
            {
                Owner = Application.Current.MainWindow
            };
            window.ShowDialog();
        }

        [RelayCommand]
        private void PedirEliminarMusica(Music music)
        {
            MusicaParaExcluir = music;
            MostrarConfirmacaoExclusao = true;
        }

        [RelayCommand]
        private void ConfirmarExclusao()
        {
            if (MusicaParaExcluir != null) EliminarMusica(MusicaParaExcluir);
            MostrarConfirmacaoExclusao = false;
            MusicaParaExcluir = null!;
        }

        [RelayCommand]
        private void CancelarExclusao()
        {
            MostrarConfirmacaoExclusao = false;
            MusicaParaExcluir = null!;
        }

        [RelayCommand]
        private void AdicionarAPlaylist(object parametro)
        {
            if (parametro is not (Music musica, Playlist playlist)) return;

            if (playlist.Name == "Favoritos")
            {
                DatabaseService.AlternarGosto(musica.Caminho, true);
                musica.Gosto = true;
                if (MusicaAtual?.Caminho == musica.Caminho)
                {
                    MusicaAtual.Gosto = true;
                    GostoAtual = true;
                }
                return;
            }

            int musicaId = DatabaseService.ObterIdMusicaPorCaminho(musica.Caminho);
            if (musicaId != -1)
            {
                DatabaseService.AdicionarMusicaAPlaylist(playlist.Id, musicaId);
            }
        }

        [RelayCommand]
        private void AlternarScrollAutomatico()
        {
            ScrollAutomaticoAtivo = !ScrollAutomaticoAtivo;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}