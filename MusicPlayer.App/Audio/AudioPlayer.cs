using NAudio.Wave;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using MusicPlayer.App.Models;

namespace MusicPlayer.App.Audio;

public class AudioPlayer : INotifyPropertyChanged, IDisposable
{
    private IWavePlayer? _waveOut;
    private AudioFileReader? _audioFileReader;
    private DispatcherTimer _timer;

    // TRUCUL SALVATOR: Un flag care știe dacă noi am oprit melodia sau s-a terminat ea singură
    private bool _isManualStop = false;

    private PlayerState _state = PlayerState.Stopped;
    public PlayerState State { get => _state; private set { _state = value; OnPropertyChanged(); } }

    private Track? _currentTrack;
    public Track? CurrentTrack { get => _currentTrack; private set { _currentTrack = value; OnPropertyChanged(); } }

    private TimeSpan _position;
    public TimeSpan Position { get => _position; private set { _position = value; OnPropertyChanged(); } }

    private float _volume = 0.5f;
    public float Volume
    {
        get => _volume;
        set { _volume = value; if (_audioFileReader != null) _audioFileReader.Volume = _volume; OnPropertyChanged(); }
    }

    public event Action? TrackEnded;
    public event PropertyChangedEventHandler? PropertyChanged;

    public AudioPlayer()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _timer.Tick += (s, e) =>
        {
            if (_audioFileReader != null && State == PlayerState.Playing)
            {
                Position = _audioFileReader.CurrentTime;
            }
        };
    }

    public void Load(Track track)
    {
        DisposeResources();
        CurrentTrack = track;
        _audioFileReader = new AudioFileReader(track.FilePath) { Volume = Volume };
        _waveOut = new WaveOutEvent();

        // Conectăm senzorul nativ
        _waveOut.PlaybackStopped += OnPlaybackStopped;

        _waveOut.Init(_audioFileReader);
        Position = TimeSpan.Zero;
        State = PlayerState.Stopped;

        // Resetăm flag-ul pentru piesa nouă
        _isManualStop = false;
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        // Dacă NU am oprit noi manual, e clar că piesa a ajuns la final singură
        if (!_isManualStop)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                State = PlayerState.Stopped;
                Position = TimeSpan.Zero;
                _timer.Stop();

                // Chemăm următoarea melodie din strategie (Next sau Repeat) instantaneu!
                TrackEnded?.Invoke();
            });
        }
    }

    public void Seek(TimeSpan time)
    {
        if (_audioFileReader != null)
        {
            _audioFileReader.CurrentTime = time;
            Position = time;
        }
    }

    public void Play()
    {
        if (_waveOut != null)
        {
            _isManualStop = false; // Cât cântă, e pe modul natural
            _waveOut.Play();
            State = PlayerState.Playing;
            _timer.Start();
        }
    }

    public void Pause()
    {
        if (_waveOut != null && State == PlayerState.Playing)
        {
            _waveOut.Pause();
            State = PlayerState.Paused;
            _timer.Stop();
        }
    }

    public void Stop()
    {
        _isManualStop = true; // Dacă folosim stop intern, setăm manual ca fiind oprit de noi
        if (_waveOut != null)
        {
            _waveOut.Stop();
            if (_audioFileReader != null) _audioFileReader.Position = 0;
            State = PlayerState.Stopped;
            Position = TimeSpan.Zero;
            _timer.Stop();
        }
    }

    private void DisposeResources()
    {
        _timer.Stop();
        if (_waveOut != null)
        {
            // OBLIGATORIU: Dezabonăm senzorul înainte de a distruge vechiul player ca să nu dea rateuri
            _waveOut.PlaybackStopped -= OnPlaybackStopped;
            _waveOut.Dispose();
            _waveOut = null;
        }
        if (_audioFileReader != null)
        {
            _audioFileReader.Dispose();
            _audioFileReader = null;
        }
    }

    public void Dispose() => DisposeResources();
    protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}