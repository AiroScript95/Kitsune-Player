using Microsoft.Extensions.DependencyInjection;
using Project_Kitsune.Models;
using Project_Kitsune.ViewModels;
using Project_Kitsune.ViewModels.ShellPages;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Project_Kitsune.Views.ShellPages
{
    /// <summary>
    /// Interaction logic for PlaylistDetailPage.xaml
    /// </summary>
    public partial class PlaylistDetailPage : UserControl
    {
        public PlaylistDetailPage()
        {
            InitializeComponent();
        }

        private void StackPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elemento && elemento.DataContext is Music musica)
            {
                if (DataContext is PlaylistDetailPageViewModel viewModel)
                {
                    viewModel.TocarMusica(musica);
                }
            }
        }

        /* MUSICA EVENTS*/

        private void ConfirmarSelecao_Click(object sender, RoutedEventArgs e)
        {
            List<Music> selecionadas = new List<Music>();
            foreach (object item in ListaTodasMusicas.SelectedItems)
            {
                if (item is Music musica)
                {
                    selecionadas.Add(musica);
                }
            }

            if (DataContext is PlaylistDetailPageViewModel viewModel)
            {
                viewModel.AdicionarMusicaEscolhida(selecionadas);
            }
        }

        private void MenuMusica_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void ListaTodasMusicas_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is PlaylistDetailPageViewModel vm)
            {
                vm.NumeroSelecionadas = ListaTodasMusicas.SelectedItems.Count;
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

                if (sender is FrameworkElement { ContextMenu: { } menu }
                    && DataContext is PlaylistDetailPageViewModel vm)
                {
                    foreach (MenuItem item in menu.Items.OfType<MenuItem>().Where(i => i.Name == "ItemRemover"))
                        item.Visibility = vm.EhComLetra ? Visibility.Collapsed : Visibility.Visible;
                }

                playerViewModel.CarregarPlaylistsDisponiveis();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void RemoverMusica_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item
                && item.Parent is ContextMenu menu
                && menu.PlacementTarget is FrameworkElement alvo
                && alvo.DataContext is Music musica
                && DataContext is PlaylistDetailPageViewModel vm)
            {
                vm.RemoverMusicaCommand.Execute(musica);
            }
        }
    }
}