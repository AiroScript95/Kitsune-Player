using Microsoft.Extensions.DependencyInjection;
using Project_Kitsune.Models;
using Project_Kitsune.Services;
using Project_Kitsune.ViewModels;
using Project_Kitsune.ViewModels.ShellPages;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

        private void ContextMenu_Opening(object sender, ContextMenuEventArgs e)
        {
            try
            {
                var playerViewModel = App.ServiceProvider.GetRequiredService<PlayerViewModel>();

                if (sender is FrameworkElement { DataContext: Music musica }
                    && musica.Caminho == playerViewModel.MusicaAtual?.Caminho)
                {
                    e.Handled = true; // música a tocar: não abre menu
                    return;
                }

                playerViewModel.CarregarPlaylistsDisponiveis();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}