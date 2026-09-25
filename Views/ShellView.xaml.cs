using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Project_Kitsune.Models;
using Project_Kitsune.ViewModels;
using IOPath = System.IO.Path;

namespace Project_Kitsune.Views
{
    public partial class ShellView : UserControl
    {
        private readonly PlayerViewModel player;
        private DispatcherTimer? _timerScroll;
        private readonly Dictionary<LinhaLetra, FrameworkElement> _mapaLinhas = new();

        public ShellView()
        {
            player = App.ServiceProvider.GetRequiredService<PlayerViewModel>();

            InitializeComponent();

            Loaded += ShellView_Loaded;
            Unloaded += ShellView_Unloaded;
        }

        /* ===== LIFECYCLE ===== */

        private void ShellView_Loaded(object sender, RoutedEventArgs e)
        {
            player.PropertyChanged += Player_PropertyChanged;
            ItemsLetras.ItemContainerGenerator.StatusChanged += ItemsLetras_StatusChanged;
        }

        private void ShellView_Unloaded(object sender, RoutedEventArgs e)
        {
            _timerScroll?.Stop();
            _timerScroll = null;

            player.PropertyChanged -= Player_PropertyChanged;
            ItemsLetras.ItemContainerGenerator.StatusChanged -= ItemsLetras_StatusChanged;
        }

        private void Player_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlayerViewModel.LinhaAtual))
            {
                AtualizarScrollLetra(player);
            }
            else if (e.PropertyName == nameof(PlayerViewModel.PosicaoAtualMs))
            {
                if (player.LetraAtual?.Sincronizada == false)
                    AtualizarScrollLetra(player);
            }
        }

        /* ===== LETRAS EVENTS / SCROLL ===== */

        private void ItemsLetras_StatusChanged(object? sender, EventArgs e)
        {
            if (ItemsLetras.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
                return;

            _mapaLinhas.Clear();
            foreach (var item in ItemsLetras.Items)
            {
                if (item is LinhaLetra linha &&
                    ItemsLetras.ItemContainerGenerator.ContainerFromItem(item) is FrameworkElement fe)
                {
                    _mapaLinhas[linha] = fe;
                }
            }
        }

        private void AtualizarScrollLetra(PlayerViewModel player)
        {
            if (player.LetraAtual == null || ScrollLetras == null) return;

            if (player.LetraAtual.Sincronizada)
            {
                if (player.LinhaAtual == null) return;
                _mapaLinhas.TryGetValue(player.LinhaAtual, out var elementoLinha);

                if (elementoLinha != null)
                {
                    Point posicaoRelativa = elementoLinha.TransformToVisual(ScrollLetras).Transform(new Point(0, 0));
                    double offsetAlvo = ScrollLetras.VerticalOffset + posicaoRelativa.Y - (ScrollLetras.ViewportHeight / 2.5) + (elementoLinha.RenderSize.Height / 2);
                    AnimarScrollSuave(ScrollLetras, Math.Max(0, offsetAlvo));
                }
                else
                {
                    int indice = player.LetraAtual.Linhas.IndexOf(player.LinhaAtual);
                    if (indice >= 0)
                    {
                        double offsetAlvo = Math.Max(0, (indice * 25) - (ScrollLetras.ViewportHeight / 2));
                        AnimarScrollSuave(ScrollLetras, offsetAlvo);
                    }
                }
            }
            else
            {
                if (player.DuracaoTotalMs <= 0) return;

                if (player.ScrollAutomaticoAtivo)
                {
                    double progresso = (double)player.PosicaoAtualMs / player.DuracaoTotalMs;
                    double offsetMaximo = ScrollLetras.ExtentHeight - ScrollLetras.ViewportHeight;

                    if (offsetMaximo <= 0) return;

                    double offsetAlvo = progresso * offsetMaximo;
                    ScrollLetras.ScrollToVerticalOffset(offsetAlvo);
                }
            }
        }

        private void AnimarScrollSuave(ScrollViewer scrollViewer, double offsetAlvo)
        {
            if (scrollViewer == null) return;

            _timerScroll?.Stop();

            double offsetInicial = scrollViewer.VerticalOffset;
            double diferenca = offsetAlvo - offsetInicial;

            if (Math.Abs(diferenca) < 1) return;

            const int passos = 8;
            int passoAtual = 0;

            _timerScroll = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };

            _timerScroll.Tick += (s, e) =>
            {
                passoAtual++;
                double progresso = (double)passoAtual / passos;
                double progressoSuavizado = 1 - Math.Pow(1 - progresso, 3);

                scrollViewer.ScrollToVerticalOffset(offsetInicial + (diferenca * progressoSuavizado));

                if (passoAtual >= passos)
                {
                    _timerScroll.Stop();
                }
            };

            _timerScroll.Start();
        }

        private async void EditarLetra_Click(object sender, RoutedEventArgs e)
        {
            Music? musicaAtual = player.MusicaAtual;
            if (musicaAtual == null) return;

            string? caminhoLrc = player.LetraAtual?.CaminhoOrigem;

            if (string.IsNullOrEmpty(caminhoLrc) || player.LetraAtual?.Fonte == FonteLetra.Embutida)
            {
                string pasta = IOPath.GetDirectoryName(musicaAtual.Caminho)!;
                string nomeBase = IOPath.GetFileNameWithoutExtension(musicaAtual.Caminho);
                caminhoLrc = IOPath.Combine(pasta, nomeBase + ".kc.lrc");
            }

            try
            {
                string fullPath = IOPath.GetFullPath(caminhoLrc);

                string? dir = IOPath.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (!File.Exists(fullPath))
                {
                    await File.WriteAllTextAsync(fullPath, string.Empty, System.Text.Encoding.UTF8);
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = "notepad.exe",
                    Arguments = $"\"{fullPath}\"",
                    UseShellExecute = true
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"Permissão negada ao criar/abrir o ficheiro: {ex.Message}");
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"Erro de I/O ao criar/abrir o ficheiro: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro inesperado ao editar letra: {ex.Message}");
            }
        }

        private void LinhaLetra_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elemento && elemento.DataContext is LinhaLetra linha)
            {
                player.SaltarParaLinhaCommand.Execute(linha);
            }
        }

        /* ===== SLIDER EVENTS ===== */

        private void Slider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1) return;

            if (sender is Slider slider)
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
            if (sender is not Slider slider) return;
            if (!player.EstaArrastando) return;

            AtualizarValorPorPosicao(slider, e.GetPosition(slider));
        }

        private void Slider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Slider slider)
            {
                if (slider.IsMouseCaptured)
                {
                    slider.ReleaseMouseCapture();
                }

                player.DefinirPosicaoManual((long)player.PosicaoAtualMs);
                player.EstaArrastando = false;
            }
        }

        /* ===== UTILS ===== */

        private static void AtualizarValorPorPosicao(Slider slider, Point posicao)
        {
            if (slider.ActualWidth <= 0) return;

            double proporcao = Math.Clamp(posicao.X / slider.ActualWidth, 0, 1);
            double novoValor = slider.Minimum + proporcao * (slider.Maximum - slider.Minimum);
            slider.Value = novoValor;
        }

        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject? parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindParent<T>(parentObject);
        }

        private void AbrirMiniPlayer_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow;
            var miniPlayer = new MiniPlayerWindow(mainWindow);
            miniPlayer.Show();
            mainWindow.Hide();
        }
    }
}