using Microsoft.Extensions.DependencyInjection;
using Project_Kitsune.Models;
using Project_Kitsune.ViewModels;
using Project_Kitsune.ViewModels.ShellPages;
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

namespace Project_Kitsune.Views.ShellPages
{
    /// <summary>
    /// Interação lógica para GrupoDetailPage.xam
    /// </summary>
    public partial class GrupoDetailPage : UserControl
    {
        public GrupoDetailPage()
        {
            InitializeComponent();
        }

        private void StackPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elemento && elemento.DataContext is Music musica)
            {
                if (DataContext is GrupoDetailPageViewModel viewModel)
                {
                    viewModel.TocarMusica(musica);
                }
            }
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