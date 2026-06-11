using System.Collections.ObjectModel;

namespace MusicPlayer.App.Models;

public enum PlayerState { Stopped, Playing, Paused }

public record Track(string Id, string Title, string Artist, string Album, TimeSpan Duration, string FilePath);

public class Playlist
{
    public ObservableCollection<Track> Tracks { get; } = new();

    public void AddTrack(Track track) => Tracks.Add(track);
    public void RemoveTrack(Track track) => Tracks.Remove(track);
    public void Clear() => Tracks.Clear();
}