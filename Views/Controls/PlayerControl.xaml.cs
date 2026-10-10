using Project_Kitsune.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Project_Kitsune.Views.Controls
{
    /// <summary>
    /// Interação lógica para PlayerControl.xam
    /// </summary>
    public partial class PlayerControl : UserControl
    {
        public PlayerControl()
        {
            InitializeComponent();
            DataContextChanged += (_, e) =>
        System.Diagnostics.Debug.WriteLine($"PlayerControl DataContext: {e.NewValue?.GetType().Name ?? "null"}");
        }

        /* ===== SLIDER EVENTS ===== */

        private void Slider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1) return;

            if (sender is Slider slider && DataContext is PlayerViewModel viewModel)
            {
                viewModel.EstaArrastando = true;
                slider.CaptureMouse(); // Prende os eventos do rato no slider

                bool clicouNoThumb = FindParent<Thumb>((DependencyObject)e.OriginalSource) != null;
                if (clicouNoThumb) return;

                AtualizarValorPorPosicao(slider, e.GetPosition(slider));
                e.Handled = true;
            }
        }

        private void Slider_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (sender is not Slider slider || DataContext is not PlayerViewModel viewModel) return;
            if (!viewModel.EstaArrastando) return;

            AtualizarValorPorPosicao(slider, e.GetPosition(slider));
        }

        private void Slider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Slider slider && DataContext is PlayerViewModel viewModel)
            {
                if (slider.IsMouseCaptured)
                {
                    slider.ReleaseMouseCapture();
                }

                viewModel.DefinirPosicaoManual((long)viewModel.PosicaoAtualMs);
                viewModel.EstaArrastando = false;
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

        private void AbrirMiniPlayer_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow;
            var miniPlayer = new MiniPlayerWindow(mainWindow);
            miniPlayer.Show();
            mainWindow.Hide();
        }
    }
}