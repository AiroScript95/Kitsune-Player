using ManagedBass;
using ManagedBass.Fx;
using ManagedBass.Mix;
using Project_Kitsune.Models;
using System.IO;
using System.Text.Json;

namespace Project_Kitsune.Services
{
    public class AudioPlayerService : IDisposable
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll",
            CharSet = System.Runtime.InteropServices.CharSet.Unicode,
            SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(
            System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool SetDllDirectory(string lpPathName);

        public event Action? MusicaTerminou;

        public event Action? ErroReproducao;

        public event Action? PlaybackStarted;

        private const int FadeDurationMs = 200;

        public bool Terminou { get; private set; }

        private int _mixerHandle;
        private int _streamAtual;
        private int _fxEqHandle;

        private readonly PeakEQParameters[] _bandasEq =
            new PeakEQParameters[10];

        private readonly Dictionary<int, SyncProcedure> _syncDelegates = new();

        private readonly SemaphoreSlim _trocaSemaphore =
            new SemaphoreSlim(1, 1);

        private long _playToken = 0;

        private int _volumeAlvo = 100;

        private List<PresetEqualizador> _presets = new();

        public List<double> _frequenciasBanda { get; set; } = new();

        public AudioPlayerService(ConfiguracaoService configuracaoService)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[EQ] Processo é 64-bit? {Environment.Is64BitProcess}");

            string nativePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Native");

            if (Directory.Exists(nativePath))
            {
                SetDllDirectory(nativePath);
            }

            if (!Bass.Init())
            {
                throw new InvalidOperationException(
                    $"Não foi possível inicializar o BASS. Erro: {Bass.LastError}");
            }

            _ = BassFx.Version;

            _mixerHandle = BassMix.CreateMixerStream(
                48000,
                2,
                BassFlags.MixerNonStop | BassFlags.Float);

            if (_mixerHandle == 0)
            {
                throw new InvalidOperationException(
                    $"Não foi possível criar o mixer. Erro: {Bass.LastError}");
            }

            _presets = CarregarPresets();

            CriarEqualizador();

            var config = configuracaoService.CarregarConfiguracao();

            if (config.EqualizadorGanhosCustom != null)
            {
                for (
                    int i = 0;
                    i < config.EqualizadorGanhosCustom.Length && i < 10;
                    i++)
                {
                    DefinirGanhoBanda(
                        (uint)i,
                        config.EqualizadorGanhosCustom[i]);
                }
            }
        }

        // ============================================================
        // EQUALIZADOR
        // ============================================================

        public List<PresetEqualizador> CarregarPresets()
        {
            string caminhoPasta = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Presets");

            string caminhoArquivo = Path.Combine(
                caminhoPasta,
                "presets.json");

            try
            {
                string textJson = File.ReadAllText(caminhoArquivo);

                var opcoes = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var presetsFile =
                    JsonSerializer.Deserialize<EqPresetsFile>(
                        textJson,
                        opcoes);

                if (presetsFile == null)
                    return new List<PresetEqualizador>();

                _frequenciasBanda = presetsFile.FrequenciesHz;

                return presetsFile.Presets
                    .Select((p, i) => new PresetEqualizador
                    {
                        Indice = (uint)i,
                        Nome = p.Name,
                        Bandas = presetsFile.FrequenciesHz
                            .Zip(
                                p.ValuesDb,
                                (freq, db) => new BandaEq
                                {
                                    Frequencia = freq,
                                    GanhoDb = db
                                })
                            .ToList()
                    })
                    .ToList();
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[EQ] Erro ao ler presets: {ex.Message}");

                return new List<PresetEqualizador>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[EQ] Erro ao carregar presets: {ex.Message}");

                return new List<PresetEqualizador>();
            }
        }

        private void CriarEqualizador()
        {
            if (_mixerHandle == 0)
                return;

            _fxEqHandle = Bass.ChannelSetFX(
                _mixerHandle,
                EffectType.PeakEQ,
                0);

            if (_fxEqHandle == 0)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[EQ] Não foi possível criar o equalizador: {Bass.LastError}");

                return;
            }

            for (int i = 0; i < 10; i++)
            {
                _bandasEq[i] = new PeakEQParameters
                {
                    lBand = i,
                    fCenter = (float)_frequenciasBanda[i],
                    fBandwidth = 1.2f,
                    fGain = 0f,
                    lChannel = FXChannelFlags.All
                };

                Bass.FXSetParameters(
                    _fxEqHandle,
                    _bandasEq[i]);
            }
        }

        public void DefinirGanhoBanda(uint banda, float valorDb)
        {
            if (banda >= 10)
                return;

            _bandasEq[banda].fGain = valorDb;

            if (_fxEqHandle != 0)
            {
                Bass.FXSetParameters(
                    _fxEqHandle,
                    _bandasEq[banda]);
            }
        }

        public void DefinirPreamp(float valorDb)
        {
            for (int i = 0; i < 10; i++)
            {
                _bandasEq[i].fGain += valorDb;

                if (_fxEqHandle != 0)
                {
                    Bass.FXSetParameters(
                        _fxEqHandle,
                        _bandasEq[i]);
                }
            }
        }

        public void DefinirEqualizador(int presetIndex)
        {
            if (presetIndex < 0 ||
                presetIndex >= _presets.Count)
                return;

            var preset = _presets[presetIndex];

            for (
                int i = 0;
                i < preset.Bandas.Count && i < 10;
                i++)
            {
                DefinirGanhoBanda(
                    (uint)i,
                    (float)preset.Bandas[i].GanhoDb);
            }
        }

        // ============================================================
        // REPRODUÇÃO
        // ============================================================

        public void TocarAudio(string path)
        {
            long meuToken =
                Interlocked.Increment(ref _playToken);

            _ = TocarAudioAsync(path, meuToken);
        }

        public async Task TocarAudioAsync(
            string path,
            long meuToken)
        {
            Terminou = false;

            int novoStream = 0;

            await _trocaSemaphore
                .WaitAsync()
                .ConfigureAwait(false);

            try
            {
                // Outra música foi solicitada enquanto esta aguardava
                if (meuToken != _playToken)
                    return;

                if (!File.Exists(path))
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[BASS] Arquivo não encontrado: {path}");

                    ErroReproducao?.Invoke();
                    return;
                }

                int streamAntigo = _streamAtual;

                float volumeAlvoFloat =
                    Math.Clamp(_volumeAlvo, 0, 100) / 100f;

                // ----------------------------------------------------
                // CRIA NOVO STREAM
                // ----------------------------------------------------

                novoStream = Bass.CreateStream(
                    path,
                    0,
                    0,
                    BassFlags.Decode | BassFlags.Float);

                if (novoStream == 0)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[BASS] CreateStream falhou: {Bass.LastError}");

                    ErroReproducao?.Invoke();
                    return;
                }

                // ----------------------------------------------------
                // ADICIONA AO MIXER
                // ----------------------------------------------------

                if (!BassMix.MixerAddChannel(
                        _mixerHandle,
                        novoStream,
                        BassFlags.MixerChanBuffer))
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[BASS] MixerAddChannel falhou: {Bass.LastError}");

                    Bass.StreamFree(novoStream);
                    novoStream = 0;

                    ErroReproducao?.Invoke();
                    return;
                }

                // Começa com volume 0 para fazer o fade-in
                Bass.ChannelSetAttribute(
                    novoStream,
                    ChannelAttribute.Volume,
                    0f);

                // ----------------------------------------------------
                // FADE-IN
                // ----------------------------------------------------

                Bass.ChannelSlideAttribute(
                    novoStream,
                    ChannelAttribute.Volume,
                    volumeAlvoFloat,
                    FadeDurationMs);

                // ----------------------------------------------------
                // FADE-OUT DA MÚSICA ANTERIOR
                // ----------------------------------------------------

                if (streamAntigo != 0)
                {
                    Bass.ChannelSlideAttribute(
                        streamAntigo,
                        ChannelAttribute.Volume,
                        0f,
                        FadeDurationMs);
                }

                // ----------------------------------------------------
                // SYNC DE FIM
                // ----------------------------------------------------

                SyncProcedure syncDelegate =
                    (int h, int channel, int data, IntPtr user) =>
                    {
                        Terminou = true;

                        if (meuToken == _playToken)
                        {
                            MusicaTerminou?.Invoke();
                        }
                    };

                _syncDelegates[novoStream] =
                    syncDelegate;

                Bass.ChannelSetSync(
                    novoStream,
                    SyncFlags.End,
                    0,
                    syncDelegate);

                // ----------------------------------------------------
                // TORNA O NOVO STREAM O STREAM ATUAL
                // ----------------------------------------------------

                _streamAtual = novoStream;

                // ----------------------------------------------------
                // CORREÇÃO PRINCIPAL
                //
                // O mixer precisa estar em reprodução.
                // MixerAddChannel NÃO inicia o mixer.
                // ----------------------------------------------------

                bool mixerTocando =
                    Bass.ChannelPlay(
                        _mixerHandle,
                        false);

                if (!mixerTocando)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[BASS] ChannelPlay do mixer falhou: {Bass.LastError}");

                    // Remove o novo stream
                    BassMix.MixerRemoveChannel(
                        novoStream);

                    _syncDelegates.Remove(
                        novoStream);

                    Bass.StreamFree(
                        novoStream);

                    if (_streamAtual == novoStream)
                        _streamAtual = streamAntigo;

                    ErroReproducao?.Invoke();
                    return;
                }

                // ----------------------------------------------------
                // AGORA SIM A REPRODUÇÃO FOI INICIADA
                // ----------------------------------------------------

                PlaybackStarted?.Invoke();

                // ----------------------------------------------------
                // ESPERA O FADE TERMINAR
                // ----------------------------------------------------

                await Task.Delay(
                    FadeDurationMs)
                    .ConfigureAwait(false);

                // Se outra música foi solicitada durante o fade,
                // não mexemos no estado da nova reprodução.
                if (meuToken != _playToken)
                    return;

                // ----------------------------------------------------
                // REMOVE STREAM ANTIGO
                // ----------------------------------------------------

                if (streamAntigo != 0 &&
                    streamAntigo != novoStream)
                {
                    BassMix.MixerRemoveChannel(
                        streamAntigo);

                    _syncDelegates.Remove(
                        streamAntigo);

                    Bass.StreamFree(
                        streamAntigo);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[AudioPlayerService] Erro em TocarAudioAsync: {ex}");

                if (novoStream != 0 &&
                    _streamAtual != novoStream)
                {
                    try
                    {
                        BassMix.MixerRemoveChannel(
                            novoStream);
                    }
                    catch
                    {
                    }

                    try
                    {
                        _syncDelegates.Remove(
                            novoStream);
                    }
                    catch
                    {
                    }

                    try
                    {
                        Bass.StreamFree(
                            novoStream);
                    }
                    catch
                    {
                    }
                }

                ErroReproducao?.Invoke();
            }
            finally
            {
                _trocaSemaphore.Release();
            }
        }

        // ============================================================
        // PAUSE / RESUME
        // ============================================================

        public void PausarAudio()
        {
            try
            {
                if (_mixerHandle == 0)
                    return;

                Bass.ChannelPause(
                    _mixerHandle);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[BASS] Erro ao pausar: {ex}");
            }
        }

        public void RetomarAudio()
        {
            try
            {
                if (_mixerHandle == 0)
                    return;

                if (_streamAtual == 0)
                    return;

                Bass.ChannelPlay(
                    _mixerHandle,
                    false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[BASS] Erro ao retomar: {ex}");
            }
        }

        public void PararAudio()
        {
            try
            {
                if (_streamAtual != 0)
                {
                    BassMix.MixerRemoveChannel(
                        _streamAtual);

                    _syncDelegates.Remove(
                        _streamAtual);

                    Bass.StreamFree(
                        _streamAtual);

                    _streamAtual = 0;
                }

                if (_mixerHandle != 0)
                {
                    Bass.ChannelStop(
                        _mixerHandle);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[BASS] Erro ao parar: {ex}");
            }
        }

        // ============================================================
        // VOLUME
        // ============================================================

        public void DefinirVolume(int vol)
        {
            _volumeAlvo =
                Math.Clamp(vol, 0, 100);

            if (_streamAtual != 0)
            {
                Bass.ChannelSetAttribute(
                    _streamAtual,
                    ChannelAttribute.Volume,
                    _volumeAlvo / 100f);
            }
        }

        public int ObterVolume()
        {
            return _volumeAlvo;
        }

        // ============================================================
        // POSIÇÃO / SEEK
        // ============================================================

        public long ObterPosicaoAtual()
        {
            if (_streamAtual == 0)
                return 0;

            try
            {
                long bytes =
                    Bass.ChannelGetPosition(
                        _streamAtual);

                if (bytes < 0)
                    return 0;

                double segundos =
                    Bass.ChannelBytes2Seconds(
                        _streamAtual,
                        bytes);

                if (double.IsNaN(segundos) ||
                    double.IsInfinity(segundos) ||
                    segundos < 0)
                {
                    return 0;
                }

                return (long)(segundos * 1000);
            }
            catch
            {
                return 0;
            }
        }

        public long ObterDuracaoTotal()
        {
            if (_streamAtual == 0)
                return 0;

            try
            {
                long bytes =
                    Bass.ChannelGetLength(
                        _streamAtual);

                if (bytes <= 0)
                    return 0;

                double segundos =
                    Bass.ChannelBytes2Seconds(
                        _streamAtual,
                        bytes);

                if (double.IsNaN(segundos) ||
                    double.IsInfinity(segundos) ||
                    segundos < 0)
                {
                    return 0;
                }

                return (long)(segundos * 1000);
            }
            catch
            {
                return 0;
            }
        }

        public void DefinirPosicao(long milissegundos)
        {
            if (_streamAtual == 0) return;

            try
            {
                if (milissegundos < 0) milissegundos = 0;

                long duracao = ObterDuracaoTotal();

                if (duracao > 0 && milissegundos > duracao)
                {
                    milissegundos = duracao;
                }

                long bytes =
                    Bass.ChannelSeconds2Bytes(_streamAtual, milissegundos / 1000.0);

                BassMix.ChannelSetPosition(_streamAtual, bytes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BASS] Erro ao definir posição: {ex}");
            }
        }

        // ============================================================
        // DISPOSE
        // ============================================================

        public void Dispose()
        {
            try
            {
                Interlocked.Increment(ref _playToken);

                PararAudio();

                if (_fxEqHandle != 0 && _mixerHandle != 0)
                {
                    Bass.ChannelRemoveFX(_mixerHandle, _fxEqHandle);
                    _fxEqHandle = 0;
                }

                if (_mixerHandle != 0)
                {
                    Bass.StreamFree(_mixerHandle);

                    _mixerHandle = 0;
                }

                _syncDelegates.Clear();

                Bass.Free();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[AudioPlayerService] Erro no Dispose: {ex}");
            }
        }
    }
}