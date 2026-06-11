using MusicPlayer.App.Audio;
using MusicPlayer.App.Models;
using System.Windows.Input;

namespace MusicPlayer.App.Commands;

// Abstractizarea Memento-ului pentru Undo/Redo cerut de WPF
public abstract class UndoableCommand : ICommand
{
    public abstract bool CanUndo { get; }
    public abstract string Description { get; }

    public event EventHandler? CanExecuteChanged;
    public abstract bool CanExecute(object? parameter);
    public abstract void Execute(object? parameter);
    public abstract void Undo();
}

public class CommandHistory
{
    private readonly Stack<UndoableCommand> _undoStack = new();
    private readonly Stack<UndoableCommand> _redoStack = new();

    public void Execute(UndoableCommand cmd, object? param = null)
    {
        cmd.Execute(param);
        if (cmd.CanUndo)
        {
            _undoStack.Push(cmd);
            _redoStack.Clear();
        }
    }

    public void Undo()
    {
        if (_undoStack.Count > 0)
        {
            var cmd = _undoStack.Pop();
            cmd.Undo();
            _redoStack.Push(cmd);
        }
    }
}

// Exemplu de comandă simplă (Play) care nu face Undo
public class PlayCommand : UndoableCommand
{
    private readonly AudioPlayer _player;
    public PlayCommand(AudioPlayer player) => _player = player;

    public override bool CanUndo => false;
    public override string Description => "Play";

    public override bool CanExecute(object? parameter) => _player.CurrentTrack != null;
    public override void Execute(object? parameter) => _player.Play();
    public override void Undo() { }
}

// Exemplu de comandă Undoable (AddTrack)
public class AddTrackCommand : UndoableCommand
{
    private readonly Playlist _playlist;
    private readonly Track _track;

    public AddTrackCommand(Playlist playlist, Track track) { _playlist = playlist; _track = track; }

    public override bool CanUndo => true;
    public override string Description => $"Add '{_track.Title}'";

    public override bool CanExecute(object? parameter) => true;
    public override void Execute(object? parameter) => _playlist.AddTrack(_track);
    public override void Undo() => _playlist.RemoveTrack(_track);
}