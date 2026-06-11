using MusicPlayer.App.Models;

namespace MusicPlayer.App.Strategies;

public interface IPlaybackStrategy
{
    string Name { get; }
    Track? GetNextTrack(Playlist playlist, Track? currentTrack);
    Track? GetPreviousTrack(Playlist playlist, Track? currentTrack);
}

public class SequentialStrategy : IPlaybackStrategy
{
    public string Name => "Sequential";

    public Track? GetNextTrack(Playlist playlist, Track? currentTrack)
    {
        if (playlist.Tracks.Count == 0) return null;
        if (currentTrack == null) return playlist.Tracks[0];

        int index = playlist.Tracks.IndexOf(currentTrack);
        if (index == -1 || index == playlist.Tracks.Count - 1) return playlist.Tracks[0];

        return playlist.Tracks[index + 1];
    }

    public Track? GetPreviousTrack(Playlist playlist, Track? currentTrack)
    {
        if (playlist.Tracks.Count == 0) return null;
        if (currentTrack == null) return playlist.Tracks[^1];

        int index = playlist.Tracks.IndexOf(currentTrack);
        if (index <= 0) return playlist.Tracks[^1];

        return playlist.Tracks[index - 1];
    }
}

public class ShuffleStrategy : IPlaybackStrategy
{
    public string Name => "Shuffle";
    private List<Track> _shuffledTracks = new();
    private int _currentIndex = -1;

    private void Reshuffle(Playlist playlist)
    {
        _shuffledTracks = playlist.Tracks.OrderBy(x => Guid.NewGuid()).ToList();
        _currentIndex = -1;
    }

    private void SyncCurrentTrack(Track? currentTrack)
    {
        if (currentTrack != null)
        {
            int realIndex = _shuffledTracks.IndexOf(currentTrack);
            if (realIndex != -1 && realIndex != _currentIndex) _currentIndex = realIndex;
        }
    }

    public Track? GetNextTrack(Playlist playlist, Track? currentTrack)
    {
        if (playlist.Tracks.Count == 0) return null;
        if (_shuffledTracks.Count != playlist.Tracks.Count) Reshuffle(playlist);
        SyncCurrentTrack(currentTrack);

        _currentIndex++;
        if (_currentIndex >= _shuffledTracks.Count)
        {
            Reshuffle(playlist);
            _currentIndex = 0;
        }
        return _shuffledTracks[_currentIndex];
    }

    public Track? GetPreviousTrack(Playlist playlist, Track? currentTrack)
    {
        if (playlist.Tracks.Count == 0) return null;
        if (_shuffledTracks.Count != playlist.Tracks.Count) Reshuffle(playlist);
        SyncCurrentTrack(currentTrack);

        _currentIndex--;
        if (_currentIndex < 0)
        {
            Reshuffle(playlist);
            _currentIndex = _shuffledTracks.Count - 1;
        }
        return _shuffledTracks[_currentIndex];
    }
}

public class SmartShuffleStrategy : IPlaybackStrategy
{
    public string Name => "Smart Shuffle";
    private readonly Random _random = new();
    private readonly Queue<Track> _history = new();
    private const int MaxHistory = 5;

    public Track? GetNextTrack(Playlist playlist, Track? currentTrack)
    {
        if (playlist.Tracks.Count == 0) return null;

        // Reducem fereastra dinamic daca playlistul are mai putin de 5 piese
        int historyLimit = Math.Min(MaxHistory, playlist.Tracks.Count - 1);
        if (historyLimit < 0) historyLimit = 0;

        while (_history.Count > historyLimit) _history.Dequeue();

        var availableTracks = playlist.Tracks.Where(t => !_history.Contains(t)).ToList();
        if (availableTracks.Count == 0) availableTracks = playlist.Tracks.ToList(); // Fallback

        var next = availableTracks[_random.Next(availableTracks.Count)];

        _history.Enqueue(next);
        while (_history.Count > historyLimit) _history.Dequeue();

        return next;
    }

    public Track? GetPreviousTrack(Playlist playlist, Track? currentTrack)
    {
        if (playlist.Tracks.Count == 0) return null;
        return playlist.Tracks[_random.Next(playlist.Tracks.Count)];
    }
}

public class RepeatOneStrategy : IPlaybackStrategy
{
    public string Name => "Repeat One";

    public Track? GetNextTrack(Playlist playlist, Track? currentTrack) => currentTrack ?? playlist.Tracks.FirstOrDefault();
    public Track? GetPreviousTrack(Playlist playlist, Track? currentTrack) => currentTrack ?? playlist.Tracks.FirstOrDefault();
}