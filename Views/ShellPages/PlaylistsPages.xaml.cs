using Project_Kitsune.Models;
using Project_Kitsune.ViewModels.ShellPages;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using static Project_Kitsune.ViewModels.ShellPages.PlaylistsPageViewModel;

namespace Project_Kitsune.Views.ShellPages
{
    /// <summary>
    /// Interaction logic for PlaylistsPages.xaml
    /// </summary>
    public partial class PlaylistsPages : UserControl
    {
        public PlaylistsPages()
        {
            InitializeComponent();
        }

        private void TextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elemento && elemento.DataContext is PlaylistComCapa item)
            {
                if (DataContext is PlaylistsPageViewModel viewModel)
                {
                    viewModel.AbrirPlaylist(item.Playlist);
                }
            }
        }
    }
}