using HandyControl.Tools;
using Microsoft.Extensions.DependencyInjection;
using Project_Kitsune.Models;
using Project_Kitsune.Services;
using Project_Kitsune.ViewModels;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Project_Kitsune.Helpers;
using System.Windows.Interop;

namespace Project_Kitsune.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /* ===== ESTADOS - Campos/Propriedades ===== */
        private readonly ShellViewModel _shellViewModel;
        private readonly SmtcService smtcService;
        private readonly ConfiguracaoService configuracaoService;
        private readonly AudioPlayerService audioPlayerService;
        private readonly PlayerViewModel player;
        private HwndSource? _hwndSource;
        private const int WM_DEVICECHANGE = 0x0219;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;

        /* ===== CONSTRUTOR ===== */

        public MainWindow()
        {
            _shellViewModel = App.ServiceProvider.GetRequiredService<ShellViewModel>();
            _shellViewModel.PropertyChanged += ShellViewModel_PropertyChanged;
            ((App)Application.Current).TemaAlterado += AtualizarCorBordaNativa;
            ((App)Application.Current).TemaAlterado += AtualizarCorTitle;

            player = App.ServiceProvider.GetRequiredService<PlayerViewModel>();
            smtcService = App.ServiceProvider.GetRequiredService<SmtcService>();
            configuracaoService = App.ServiceProvider.GetRequiredService<ConfiguracaoService>();
            audioPlayerService = App.ServiceProvider.GetRequiredService<AudioPlayerService>();

            Configuracao configuracao = configuracaoService.CarregarConfiguracao();

            InitializeComponent();

            if (configuracao.JanelaLargura != 0) this.Width = configuracao.JanelaLargura;
            if (configuracao.JanelaAltura != 0) this.Height = configuracao.JanelaAltura;

            double x = configuracao.JanelaX;
            double y = configuracao.JanelaY;

            bool estaNaTela = x >= SystemParameters.VirtualScreenLeft &&
                              x < (SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth) &&
                              y >= SystemParameters.VirtualScreenTop &&
                              y < (SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight);

            if (estaNaTela && (x != 0 || y != 0))
            {
                this.WindowStartupLocation = WindowStartupLocation.Manual;
                this.Top = y;
                this.Left = x;
            }
            else
            {
                this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            this.Closing += MainWindow_Closing;

            ConfigHelper.Instance.SetLang("en");
        }

        /* ===== LIFECYCLE OVERRIDES ===== */

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            smtcService?.Inicializar(this, player);
            DwmHelper.AtivarCantosArredondados(this);
            AtualizarCorBordaNativa();
            AtualizarCorTitle();

            if (PresentationSource.FromVisual(this) is HwndSource source)
            {
                _hwndSource = source;
                _hwndSource.AddHook(WndProc);
            }
        }

        private async Task WndProc_TratarDeviceArrivalAsync()
        {
            try
            {
                BibliotecaService bibliotecaService = App.ServiceProvider.GetRequiredService<BibliotecaService>();
                Configuracao config = configuracaoService.CarregarConfiguracao();
                await bibliotecaService.GarantirBibliotecaCarregadaAsync(config.RotaMusica, forcar: true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao atualizar biblioteca após dispositivo: {ex}");
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_DEVICECHANGE)
            {
                int evento = wParam.ToInt32();

                if (evento == DBT_DEVICEARRIVAL)
                {
                    _ = WndProc_TratarDeviceArrivalAsync();
                }
            }

            return IntPtr.Zero;
        }

        private void AtualizarCorTitle()
        {
            if (Application.Current.Resources["SurfaceBrush"] is SolidColorBrush brush)
            {
                DwmHelper.DefinirCorTitleBar(this, brush.Color);
            }
        }

        private void AtualizarCorBordaNativa()
        {
            string chaveRecurso = _shellViewModel.Border switch
            {
                "Normal" => "BorderBrush2",
                "Color" => "AccentBrush",
                "Color2" => "AccentAltBrush",
                "Color3" => "AccentTertiaryBrush",
                _ => "BorderBrush2"
            };

            if (TryFindResource(chaveRecurso) is SolidColorBrush brush)
            {
                DwmHelper.DefinirCorBorda(this, brush.Color);
            }
        }

        private void ShellViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ShellViewModel.Border))
                AtualizarCorBordaNativa();
        }

        /* ===== UTILS ===== */

        private void SalvarDados()
        {
            Configuracao configuracao = configuracaoService.CarregarConfiguracao();
            configuracao.JanelaLargura = this.RestoreBounds.Width;
            configuracao.JanelaAltura = this.RestoreBounds.Height;
            configuracao.JanelaY = this.RestoreBounds.Top;
            configuracao.JanelaX = this.RestoreBounds.Left;

            if (player.MusicaAtual != null)
            {
                configuracao.UltimaMusica ??= new UltimaMusicaInfo();
                configuracao.UltimaMusica.Caminho = player.MusicaAtual.Caminho;
                configuracao.UltimaMusica.PosicaoMs = audioPlayerService.ObterPosicaoAtual();
                configuracao.UltimaMusica.DuracaoMS = audioPlayerService.ObterDuracaoTotal();
                configuracao.UltimaMusica.EstavaTocando = player.EstaTocando;
            }

            configuracaoService.GuardarConfiguracao(configuracao);
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _shellViewModel.PropertyChanged -= ShellViewModel_PropertyChanged;
            ((App)Application.Current).TemaAlterado -= AtualizarCorBordaNativa;
            ((App)Application.Current).TemaAlterado -= AtualizarCorTitle;

            if (_hwndSource != null)
            {
                _hwndSource.RemoveHook(WndProc);
                _hwndSource = null;
            }
            SalvarDados();
        }
    }
}