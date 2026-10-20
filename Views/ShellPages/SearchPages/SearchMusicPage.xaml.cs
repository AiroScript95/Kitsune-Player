using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Extensions.DependencyInjection;
using Project_Kitsune.Models;
using Project_Kitsune.ViewModels;
using Project_Kitsune.ViewModels.ShellPages.SearchPages;

namespace Project_Kitsune.Views.ShellPages.SearchPages
{
    /// <summary>
    /// Interação lógica para SearchMusicPage.xam
    /// </summary>
    public partial class SearchMusicPage : UserControl
    {
        public SearchMusicPage()
        {
            InitializeComponent();
        }

        private void Music_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elemento && elemento.DataContext is Music musica) {
                if (DataContext is SearchMusicPageViewModel viewModel) {
                    viewModel.TocarMusica(musica);
                }
            }
        }

        private void ContextMenu_Opening(object sender, ContextMenuEventArgs e)
        {
            try {
                var playerViewModel = App.ServiceProvider.GetRequiredService<PlayerViewModel>();

                if (sender is FrameworkElement { DataContext: Music musica }
                    && musica.Caminho == playerViewModel.MusicaAtual?.Caminho) {
                    e.Handled = true; // música a tocar: não abre menu
                    return;
                }

                playerViewModel.CarregarPlaylistsDisponiveis();
            }
            catch (Exception ex) {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}