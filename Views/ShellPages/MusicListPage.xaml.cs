using HandyControl.Tools.Extension;
using Microsoft.Extensions.DependencyInjection;
using Project_Kitsune.Models;
using Project_Kitsune.Services;
using Project_Kitsune.ViewModels;
using Project_Kitsune.ViewModels.ShellPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Project_Kitsune.Views.ShellPages
{
    /// <summary>
    /// Interaction logic for MusicListPage.xaml
    /// </summary>
    public partial class MusicListPage : UserControl
    {
        protected ConfiguracaoService _config;

        public MusicListPage()
        {
            InitializeComponent();
            _config = App.ServiceProvider.GetRequiredService<ConfiguracaoService>();
            Configuracao configuracao = _config.CarregarConfiguracao();
            switch (configuracao.OrdemMusicList)
            {
                case "Alfabetica":
                    RadioAlfabetica.IsChecked = true;
                    break;

                case "MaisRecente":
                    RadioMaisRecente.IsChecked = true;
                    break;

                case "MaisTocada":
                    RadioMaisTocada.IsChecked = true;
                    break;

                case "Aleatorio":
                    RadioAleatorio.IsChecked = true;
                    break;

                case "Duracao":
                    RadioDuracao.IsChecked = true;
                    break;

                default:
                    RadioAlfabetica.IsChecked = true;
                    break;
            }
            if (configuracao.OrdemDescendente) RadioDescendete.IsChecked = true;
            else RadioAscendente.IsChecked = true;
        }

        /* MUSIC EVENTS */

        private void StackPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elemento && elemento.DataContext is Music musica)
            {
                if (DataContext is MusicListPageViewModel viewModel)
                {
                    viewModel.TocarMusica(musica);
                }
            }
        }

        private void Ordenacao_Changed(object sender, RoutedEventArgs e)
        {
            if (DataContext is not MusicListPageViewModel vm) return;

            string ObterCriterioAtual()
            {
                if (RadioAlfabetica.IsChecked == true) return "Alfabetica";
                if (RadioMaisRecente.IsChecked == true) return "MaisRecente";
                if (RadioMaisTocada.IsChecked == true) return "MaisTocada";
                if (RadioDuracao.IsChecked == true) return "Duracao";
                return "Aleatorio";
            }

            bool DirecaoPadraoPara(string criterio)
            {
                return criterio switch
                {
                    "MaisRecente" => true,   // mais recentes primeiro
                    "MaisTocada" => true,    // mais tocadas primeiro
                    "Duracao" => true,       // (opcional) duracao maior primeiro
                    "Alfabetica" => false,   // A..Z
                    _ => false
                };
            }

            if (sender is RadioButton rb)
            {
                // Se o usuário clicou em um radio de CRITÉRIO
                if (rb.GroupName == "Ordenacao")
                {
                    string criterio = ObterCriterioAtual();
                    bool defaultDesc = DirecaoPadraoPara(criterio);

                    // Atualiza visuais de direção para refletir o default
                    RadioDescendete.IsChecked = defaultDesc;
                    RadioAscendente.IsChecked = !defaultDesc;

                    // Chama o ViewModel com o critério e a direção padrão
                    vm.MudarOrdenacao(criterio, defaultDesc);
                    return;
                }

                // Se o usuário clicou em um radio de DIREÇÃO
                if (rb.GroupName == "Direcao")
                {
                    string criterio = ObterCriterioAtual();
                    bool ordemDesc = RadioDescendete.IsChecked == true;
                    vm.MudarOrdenacao(criterio, ordemDesc);
                    return;
                }
            }

            // Caso genérico — reaplica com o estado atual dos radios
            string criterioAtual = ObterCriterioAtual();
            bool ordem = RadioDescendete.IsChecked == true;
            vm.MudarOrdenacao(criterioAtual, ordem);
        }

        /* ===== SLIDER EVENTS ===== */

        private void Slider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1) return;

            if (sender is Slider slider && DataContext is MusicListPageViewModel viewModel)
            {
                viewModel.PlayerViewModel.EstaArrastando = true;
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
            if (sender is not Slider slider || DataContext is not MusicListPageViewModel viewModel) return;
            if (!viewModel.PlayerViewModel.EstaArrastando) return;

            AtualizarValorPorPosicao(slider, e.GetPosition(slider));
        }

        private void Slider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Slider slider && DataContext is MusicListPageViewModel viewModel)
            {
                if (slider.IsMouseCaptured)
                {
                    slider.ReleaseMouseCapture();
                }

                viewModel.PlayerViewModel.DefinirPosicaoManual((long)viewModel.PlayerViewModel.PosicaoAtualMs);
                viewModel.PlayerViewModel.EstaArrastando = false;
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

        private void ContextMenu_Opening(object sender, ContextMenuEventArgs e)
        {
            try
            {
                var playerViewModel = App.ServiceProvider.GetRequiredService<PlayerViewModel>();
                playerViewModel.CarregarPlaylistsDisponiveis();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}