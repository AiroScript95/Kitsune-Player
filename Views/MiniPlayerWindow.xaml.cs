using HandyControl.Tools;
using Microsoft.Extensions.DependencyInjection;
using Project_Kitsune.Helpers;
using Project_Kitsune.Models;
using Project_Kitsune.Services;
using Project_Kitsune.ViewModels;
using Project_Kitsune.ViewModels.ShellPages;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace Project_Kitsune.Views
{
    public partial class MiniPlayerWindow : Window
    {
        private readonly PlayerViewModel player;
        private readonly ShellViewModel _shellViewModel;
        private readonly ConfiguracaoService configuracaoService;
        private readonly Window _mainWindow;

        public MiniPlayerWindow(Window mainWindow)
        {
            player = App.ServiceProvider.GetRequiredService<PlayerViewModel>();
            _shellViewModel = App.ServiceProvider.GetRequiredService<ShellViewModel>();
            configuracaoService = App.ServiceProvider.GetRequiredService<ConfiguracaoService>();
            _mainWindow = mainWindow;

            _shellViewModel.PropertyChanged += ShellViewModel_PropertyChanged;
            ((App)Application.Current).TemaAlterado += AtualizarCorBordaNativa;
            ((App)Application.Current).TemaAlterado += AtualizarCorTitle;

            InitializeComponent();

            DataContext = player;
            Topmost = true;

            this.Loaded += MiniPlayerWindow_Loaded;
            this.Closing += MiniPlayerWindow_Closing;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            DwmHelper.AtivarCantosArredondados(this);
            AtualizarCorBordaNativa();
            AtualizarCorTitle();
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

        private void MiniPlayerWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _shellViewModel.PropertyChanged -= ShellViewModel_PropertyChanged;
            ((App)Application.Current).TemaAlterado -= AtualizarCorBordaNativa;
            ((App)Application.Current).TemaAlterado -= AtualizarCorTitle;
        }

        private void MiniPlayerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Configuracao configuracao = configuracaoService.CarregarConfiguracao();

            double x = configuracao.MiniJanelaX;
            double y = configuracao.MiniJanelaY;
            double margin = 10;

            bool estaNaTela = x >= SystemParameters.VirtualScreenLeft &&
                              x < (SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth) &&
                              y >= SystemParameters.VirtualScreenTop &&
                              y < (SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight);

            if (estaNaTela && (x != 0 || y != 0))
            {
                this.Left = x;
                this.Top = y;
            }
            else
            {
                // Posiciona no canto inferior direito
                this.Left = SystemParameters.WorkArea.Width - this.ActualWidth - margin;
                this.Top = SystemParameters.WorkArea.Height - this.ActualHeight - margin;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Salva sempre a posição, independente do estado da janela principal
            Configuracao configuracao = configuracaoService.CarregarConfiguracao();
            configuracao.MiniJanelaX = this.Left;
            configuracao.MiniJanelaY = this.Top;
            configuracaoService.GuardarConfiguracao(configuracao);

            if (!_mainWindow.IsVisible)
            {
                _mainWindow.Show();
            }
        }

        /* ===== SLIDER EVENTS ===== */

        private void Slider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1) return;

            if (sender is Slider slider && DataContext is PlayerViewModel)
            {
                player.EstaArrastando = true;
                slider.CaptureMouse();

                bool clicouNoThumb = FindParent<Thumb>((DependencyObject)e.OriginalSource) != null;
                if (clicouNoThumb) return;

                AtualizarValorPorPosicao(slider, e.GetPosition(slider));
                e.Handled = true;
            }
        }

        private void Slider_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (sender is not Slider slider || DataContext is not PlayerViewModel) return;
            if (!player.EstaArrastando) return;

            AtualizarValorPorPosicao(slider, e.GetPosition(slider));
        }

        private void Slider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Slider slider && DataContext is PlayerViewModel)
            {
                if (slider.IsMouseCaptured)
                {
                    slider.ReleaseMouseCapture();
                }
                player.DefinirPosicaoManual((long)player.PosicaoAtualMs);
                player.EstaArrastando = false;
            }
        }

        /* ===== HELPERS ===== */

        private static void AtualizarValorPorPosicao(Slider slider, Point posicao)
        {
            double proporcao = posicao.X / slider.ActualWidth;
            double novoValor = proporcao * slider.Maximum;
            slider.Value = Math.Max(slider.Minimum, Math.Min(slider.Maximum, novoValor));
        }

        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject? parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindParent<T>(parentObject);
        }
    }
}