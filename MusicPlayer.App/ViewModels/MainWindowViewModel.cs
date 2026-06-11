using Microsoft.Win32;
using MusicPlayer.App.Audio;
using MusicPlayer.App.Models;
using MusicPlayer.App.Strategies;
using NAudio.Wave;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;

namespace MusicPlayer.App.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    public Playlist MainPlaylist { get; } = new();
    public AudioPlayer Player { get; } = new();

    private IPlaybackStrategy _currentStrategy = new SequentialStrategy();

    private int _skips;
    public int Skips { get => _skips; set { _skips = value; OnPropertyChanged(); } }

    private TimeSpan _totalPlayed = TimeSpan.Zero;
    public string TotalPlayedString => $"{(int)_totalPlayed.TotalHours}h {_totalPlayed.Minutes}m {_totalPlayed.Seconds}s";

    public ObservableCollection<string> ActionHistory { get; } = new();
    private readonly Stack<Track> _undoStack = new();
    private readonly Stack<Track> _redoStack = new();

    private Track? _selectedTrack;
    public Track? SelectedTrack
    {
        get => _selectedTrack;
        set
        {
            if (_selectedTrack != null && Player.Position.TotalSeconds > 0 && Player.Position.TotalSeconds < 30)
            {
                Skips++;
            }

            _selectedTrack = value;
            OnPropertyChanged();
            if (value != null)
            {
                Player.Load(value);
                Player.Play();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public double SeekPosition
    {
        get => Player.Position.TotalSeconds;
        set { if (Math.Abs(Player.Position.TotalSeconds - value) > 0.5) Player.Seek(TimeSpan.FromSeconds(value)); }
    }

    public ICommand PlayCmd { get; }
    public ICommand PrevCmd { get; }
    public ICommand PauseCmd { get; }
    public ICommand NextCmd { get; }
    public ICommand AddFilesCmd { get; }

    public ICommand UndoCmd { get; }
    public ICommand RedoCmd { get; }
    public ICommand ChangeStrategyCmd { get; }

    public MainWindowViewModel()
    {
        PlayCmd = new RelayCommand(_ => { if (Player.CurrentTrack != null) Player.Play(); else if (SelectedTrack != null) { Player.Load(SelectedTrack); Player.Play(); } else if (MainPlaylist.Tracks.Count > 0) SelectedTrack = MainPlaylist.Tracks[0]; });
        PrevCmd = new RelayCommand(_ => PlayPreviousTrack());
        PauseCmd = new RelayCommand(_ => Player.Pause());
        NextCmd = new RelayCommand(_ => PlayNextTrack());

        AddFilesCmd = new RelayCommand(_ => AddFiles());
        UndoCmd = new RelayCommand(_ => UndoAction(), _ => _undoStack.Count > 0);
        RedoCmd = new RelayCommand(_ => RedoAction(), _ => _redoStack.Count > 0);
        ChangeStrategyCmd = new RelayCommand(strategyName => SetStrategy(strategyName?.ToString()));

        Player.TrackEnded += PlayNextTrack;

        var statsTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        statsTimer.Tick += (s, e) =>
        {
            if (Player.State == PlayerState.Playing)
            {
                _totalPlayed = _totalPlayed.Add(TimeSpan.FromSeconds(1));
                OnPropertyChanged(nameof(TotalPlayedString));
                OnPropertyChanged(nameof(SeekPosition));
            }
        };
        statsTimer.Start();
    }

    private void PlayNextTrack()
    {
        var next = _currentStrategy.GetNextTrack(MainPlaylist, Player.CurrentTrack);
        if (next != null) SelectedTrack = next;
    }

    private void PlayPreviousTrack()
    {
        var prev = _currentStrategy.GetPreviousTrack(MainPlaylist, Player.CurrentTrack);
        if (prev != null) SelectedTrack = prev;
    }

    private void SetStrategy(string? name)
    {
        // ACUM AVEM CELE 4 STRATEGII LEGATE CORECT LA BUTOANELE LOR:
        if (name == "Shuffle") _currentStrategy = new ShuffleStrategy();
        else if (name == "SmartShuffle") _currentStrategy = new SmartShuffleStrategy();
        else if (name == "RepeatOne") _currentStrategy = new RepeatOneStrategy();
        else _currentStrategy = new SequentialStrategy();

        LogHistory($"↶ Changed Strategy to {_currentStrategy.Name}");
    }

    private void AddFiles()
    {
        var ofd = new OpenFileDialog { Multiselect = true, Filter = "Audio Files|*.mp3;*.wav" };
        if (ofd.ShowDialog() == true)
        {
            foreach (var file in ofd.FileNames)
            {
                using var reader = new AudioFileReader(file);
                var track = new Track(Guid.NewGuid().ToString(), System.IO.Path.GetFileNameWithoutExtension(file), "Unknown", "Unknown", reader.TotalTime, file);

                MainPlaylist.AddTrack(track);
                _undoStack.Push(track);
                _redoStack.Clear();
                LogHistory($"↶ Added '{track.Title}'");
            }
        }
    }

    private void UndoAction()
    {
        if (_undoStack.Count > 0)
        {
            var track = _undoStack.Pop();
            MainPlaylist.RemoveTrack(track);
            _redoStack.Push(track);
            LogHistory($"↶ UNDO: Removed '{track.Title}'");
            if (SelectedTrack == track) Player.Stop();
        }
    }

    private void RedoAction()
    {
        if (_redoStack.Count > 0)
        {
            var track = _redoStack.Pop();
            MainPlaylist.AddTrack(track);
            _undoStack.Push(track);
            LogHistory($"↷ REDO: Added '{track.Title}'");
        }
    }

    private void LogHistory(string message)
    {
        ActionHistory.Insert(0, message);
        if (ActionHistory.Count > 50) ActionHistory.RemoveAt(ActionHistory.Count - 1);
        CommandManager.InvalidateRequerySuggested();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;
    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null) { _execute = execute; _canExecute = canExecute; }
    public event EventHandler? CanExecuteChanged { add => CommandManager.RequerySuggested += value; remove => CommandManager.RequerySuggested -= value; }
    public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);
    public void Execute(object? parameter) => _execute(parameter);
}