using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Project_Kitsune.Models;
using Project_Kitsune.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace Project_Kitsune.ViewModels
{
    public partial class MainViewModel : INotifyPropertyChanged
    {
        /* ESTADOS - (Campos/Propriedades) */
        public PlayerViewModel Player { get; set; }
        protected ConfiguracaoService configuracaoService;

        private object? _currentView;

        public object? CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        protected ShellViewModel _shell;
        public string Border => _shell.Border;

        public MainViewModel()
        {
            Player = App.ServiceProvider.GetRequiredService<PlayerViewModel>();
            _shell = App.ServiceProvider.GetRequiredService<ShellViewModel>();
            _shell.PropertyChanged += (s, e) =>
                                {
                                    if (e.PropertyName == nameof(ShellViewModel.Border))
                                        OnPropertyChanged(nameof(Border));
                                };
            configuracaoService = App.ServiceProvider.GetRequiredService<ConfiguracaoService>(); ;
            InicializarAplicacao();
        }

        /* METODOS */

        private void InicializarAplicacao()
        {
            LoadingViewModel loading = new LoadingViewModel();
            loading.Terminado += () => CurrentView = App.ServiceProvider.GetRequiredService<ShellViewModel>();
            CurrentView = loading;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}