using System;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using Windows.Media;
using Windows.Storage.Streams;
using Project_Kitsune.Models;
using Project_Kitsune.ViewModels;

namespace Project_Kitsune.Services
{
    public class SmtcService : IDisposable
    {
        private SystemMediaTransportControls _smtc = null!;
        private PlayerViewModel _player = null!;

        public void Inicializar(Window janela, PlayerViewModel player)
        {
            _player = player;

            var hwnd = new WindowInteropHelper(janela).Handle;
            _smtc = SystemMediaTransportControlsInterop.GetForWindow(hwnd);

            _smtc.IsEnabled = true;
            _smtc.IsPlayEnabled = true;
            _smtc.IsPauseEnabled = true;
            _smtc.IsNextEnabled = true;
            _smtc.IsPreviousEnabled = true;

            // Registra os ouvintes do sistema
            _smtc.ButtonPressed += Smtc_ButtonPressed;
            _smtc.ShuffleEnabledChangeRequested += Smtc_ShuffleModeChangeRequested;
            _smtc.AutoRepeatModeChangeRequested += Smtc_AutoRepeatModeChangeRequested;
            _smtc.PlaybackPositionChangeRequested += Smtc_PlaybackPositionChangeRequested;

            _player.PropertyChanged += Player_PropertyChanged;
        }

        private async void Smtc_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                switch (args.Button)
                {
                    case SystemMediaTransportControlsButton.Play:
                        _player.Retomar();
                        break;

                    case SystemMediaTransportControlsButton.Pause:
                        _player.Pausar();
                        break;

                    case SystemMediaTransportControlsButton.Next:
                        if (_player.ProximaCommand.CanExecute(null))
                            _player.ProximaCommand.Execute(null);
                        break;

                    case SystemMediaTransportControlsButton.Previous:
                        if (_player.AnteriorCommand.CanExecute(null))
                            _player.AnteriorCommand.Execute(null);
                        break;
                }
            });
        }

        private async void Smtc_ShuffleModeChangeRequested(SystemMediaTransportControls sender, ShuffleEnabledChangeRequestedEventArgs args)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _smtc.ShuffleEnabled = args.RequestedShuffleEnabled;
            });
        }

        private async void Smtc_AutoRepeatModeChangeRequested(SystemMediaTransportControls sender, AutoRepeatModeChangeRequestedEventArgs args)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _smtc.AutoRepeatMode = args.RequestedAutoRepeatMode;
            });
        }

        private async void Smtc_PlaybackPositionChangeRequested(SystemMediaTransportControls sender, PlaybackPositionChangeRequestedEventArgs args)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _player.PosicaoAtualMs = args.RequestedPlaybackPosition.TotalMilliseconds;
            });
        }

        private void Player_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlayerViewModel.MusicaAtual))
            {
                AtualizarMetadados(_player.MusicaAtual);
            }
            else if (e.PropertyName == nameof(PlayerViewModel.EstaTocando))
            {
                _smtc.PlaybackStatus = _player.EstaTocando
                    ? MediaPlaybackStatus.Playing
                    : MediaPlaybackStatus.Paused;
            }
            else if (e.PropertyName == nameof(PlayerViewModel.PosicaoAtualMs) || e.PropertyName == nameof(PlayerViewModel.DuracaoTotalMs))
            {
                AtualizarTimeline();
            }
        }

        private async void AtualizarMetadados(Music? musica)
        {
            var updater = _smtc.DisplayUpdater;

            if (musica == null)
            {
                updater.ClearAll();
                _smtc.PlaybackStatus = MediaPlaybackStatus.Stopped;
                updater.Update();
                return;
            }

            updater.Type = MediaPlaybackType.Music;
            updater.AppMediaId = "ProjectKitsune";
            updater.MusicProperties.Title = musica.Titulo ?? string.Empty;
            updater.MusicProperties.Artist = musica.Artista ?? string.Empty;
            updater.MusicProperties.AlbumTitle = musica.Album ?? string.Empty;

            if (musica.Image != null && musica.Image.Length > 0)
            {
                try
                {
                    var stream = new InMemoryRandomAccessStream();
                    using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
                    {
                        writer.WriteBytes(musica.Image);
                        await writer.StoreAsync();
                        await writer.FlushAsync();
                        writer.DetachStream();
                    }
                    stream.Seek(0);
                    updater.Thumbnail = RandomAccessStreamReference.CreateFromStream(stream);
                }
                catch
                {
                    updater.Thumbnail = null;
                }
            }
            else
            {
                updater.Thumbnail = null;
            }

            updater.Update();
            AtualizarTimeline();
        }

        public void AtualizarTimeline()
        {
            if (_player.MusicaAtual == null || _player.DuracaoTotalMs <= 0) return;

            var timelineProperties = new SystemMediaTransportControlsTimelineProperties
            {
                StartTime = TimeSpan.Zero,
                MinSeekTime = TimeSpan.Zero,
                Position = TimeSpan.FromMilliseconds(_player.PosicaoAtualMs),
                EndTime = TimeSpan.FromMilliseconds(_player.DuracaoTotalMs),
                MaxSeekTime = TimeSpan.FromMilliseconds(_player.DuracaoTotalMs)
            };

            _smtc.UpdateTimelineProperties(timelineProperties);
        }

        public void Dispose()
        {
            if (_smtc != null)
            {
                _smtc.ButtonPressed -= Smtc_ButtonPressed;
                _smtc.ShuffleEnabledChangeRequested -= Smtc_ShuffleModeChangeRequested;
                _smtc.AutoRepeatModeChangeRequested -= Smtc_AutoRepeatModeChangeRequested;
                _smtc.PlaybackPositionChangeRequested -= Smtc_PlaybackPositionChangeRequested;
            }

            if (_player != null)
            {
                _player.PropertyChanged -= Player_PropertyChanged;
            }

            GC.SuppressFinalize(this);
        }
    }
}