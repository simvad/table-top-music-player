using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using ModernMusicPlayer.Services;
using ModernMusicPlayer.Repositories;
using ModernMusicPlayer.Entities;
using ReactiveUI;
using System.Reactive.Linq;
using System.Reactive.Disposables;

namespace ModernMusicPlayer.ViewModels
{
    public class MainViewModel : ReactiveObject, IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        
        public PlaybackViewModel PlaybackViewModel { get; }
        public TrackManagementViewModel TrackManagementViewModel { get; }
        public TagManagementViewModel TagManagementViewModel { get; }
        public SearchViewModel SearchViewModel { get; }
        public ISessionService SessionService { get; }

        private bool _isSettingsOpen;
        public bool IsSettingsOpen
        {
            get => _isSettingsOpen;
            set => this.RaiseAndSetIfChanged(ref _isSettingsOpen, value);
        }

        private bool _isSessionPanelOpen;
        public bool IsSessionPanelOpen
        {
            get => _isSessionPanelOpen;
            set => this.RaiseAndSetIfChanged(ref _isSessionPanelOpen, value);
        }

        private bool _isAddTrackOpen;
        public bool IsAddTrackOpen
        {
            get => _isAddTrackOpen;
            set => this.RaiseAndSetIfChanged(ref _isAddTrackOpen, value);
        }

        private bool _isEditTagsOpen;
        public bool IsEditTagsOpen
        {
            get => _isEditTagsOpen;
            set => this.RaiseAndSetIfChanged(ref _isEditTagsOpen, value);
        }

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
        }

        public ICommand OpenAddTrackCommand { get; }
        public ICommand OpenSessionPanelCommand { get; }
        public ICommand ClearErrorCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand PlayTrackCommand { get; }
        public ICommand EditTrackTagsCommand { get; }
        public ICommand DeleteTrackCommand { get; }
        public ICommand CloseAddTrackCommand { get; }
        public ICommand CloseEditTagsCommand { get; }
        public ICommand CloseSettingsCommand { get; }

        public MainViewModel(
            AudioPlayerService audioPlayer,
            ITrackRepository trackRepository,
            ITagRepository tagRepository,
            ISessionService sessionService)
        {
            SessionService = sessionService;

            // Initialize child view models
            TrackManagementViewModel = new TrackManagementViewModel(trackRepository, audioPlayer);
            TagManagementViewModel = new TagManagementViewModel(tagRepository, trackRepository);
            PlaybackViewModel = new PlaybackViewModel(audioPlayer, sessionService);
            SearchViewModel = new SearchViewModel(TrackManagementViewModel.AllTracks);

            // Initialize commands
            OpenAddTrackCommand = ReactiveCommand.Create(() => IsAddTrackOpen = true);
            OpenSessionPanelCommand = ReactiveCommand.Create(() => IsSessionPanelOpen = true);
            ClearErrorCommand = ReactiveCommand.Create(() => ErrorMessage = null);
            OpenSettingsCommand = ReactiveCommand.Create(() => IsSettingsOpen = true);
            PlayTrackCommand = ReactiveCommand.Create<TrackEntity>(track => PlaybackViewModel.PlayTrack(track));
            EditTrackTagsCommand = ReactiveCommand.Create<TrackEntity>(track => 
            {
                TagManagementViewModel.EditingTrack = track;
                IsEditTagsOpen = true;
            });
            DeleteTrackCommand = TrackManagementViewModel.DeleteTrackCommand;
            CloseAddTrackCommand = ReactiveCommand.Create(() => IsAddTrackOpen = false);
            CloseEditTagsCommand = ReactiveCommand.Create(() => IsEditTagsOpen = false);
            CloseSettingsCommand = ReactiveCommand.Create(() => IsSettingsOpen = false);

            // Wire up events between view models
            PlaybackViewModel.TrackPlayed += async (s, track) => 
            {
                await TrackManagementViewModel.UpdateTrackStatistics(track.Id);
            };

            TrackManagementViewModel.TracksChanged += (s, e) => 
            {
                SearchViewModel.RefreshDisplayedTracks();
            };

            TagManagementViewModel.TagsChanged += (s, e) => 
            {
                SearchViewModel.RefreshDisplayedTracks();
            };

            SearchViewModel.FilteredTracksChanged += (s, filteredTracks) => 
            {
                PlaybackViewModel.UpdatePlayQueue(filteredTracks);
            };

            // Subscribe to session events
            SessionService.SessionEnded
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    if (IsSessionPanelOpen)
                    {
                        IsSessionPanelOpen = false;
                    }
                    ErrorMessage = "Session ended";
                })
                .DisposeWith(_disposables);

            // Monitor client join/leave for host
            SessionService.ClientJoined
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(clientId =>
                {
                    if (SessionService.IsHost)
                    {
                        ErrorMessage = $"Client {clientId} joined the session";
                    }
                })
                .DisposeWith(_disposables);

            SessionService.ClientLeft
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(clientId =>
                {
                    if (SessionService.IsHost)
                    {
                        ErrorMessage = $"Client {clientId} left the session";
                    }
                })
                .DisposeWith(_disposables);
        }

        public async void Dispose()
        {
            // Cleanup session if active
            if (SessionService.IsConnected)
            {
                try
                {
                    await SessionService.LeaveSession();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during session cleanup: {ex.Message}");
                }
            }

            PlaybackViewModel?.Dispose();
            _disposables.Dispose();
        }
    }
}