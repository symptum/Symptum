namespace Symptum.UI.Controls;

using Windows.Media.Core;
using Windows.Media.Playback;

public partial class AudioVisualizer
{
    private MediaPlayer? _mediaPlayer;
    private bool _sessionSubscribed;
    private double _lastUiPosition;

    private void InitializePlayer()
    {
        if (_mediaPlayer != null)
        {
            return;
        }

        _mediaPlayer = new()
        {
            AutoPlay = false,
        };
        _mediaPlayer.CurrentStateChanged += OnPlayerCurrentStateChanged;
        _mediaPlayer.MediaEnded += OnPlayerMediaEnded;
        _mediaPlayer.MediaOpened += OnPlayerMediaOpened;
    }

    private void SetSource(StorageFile? file)
    {
        InitializePlayer();
        if (_mediaPlayer == null)
        {
            return;
        }

        UnsubscribeSessionEvents();
        _mediaPlayer.Source = file != null ? MediaSource.CreateFromUri(GetMediaUri(file)) : null;
        SubscribeSessionEvents();
    }

    private static Uri? GetMediaUri(StorageFile file)
    {
        var path = file.Path;
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (path.Contains("://", StringComparison.Ordinal))
        {
            return new Uri(path);
        }

        // Not sure how this is going to work cross platform.
        return new Uri(new Uri("file:///", UriKind.Absolute), path.Replace('\\', '/'));
    }

    private void PlayCore()
    {
        if (_mediaPlayer == null) return;

        if (_mediaEnded && _mediaPlayer?.PlaybackSession != null)
        {
            _mediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
        }

        _mediaPlayer?.Play();
    }

    private void PauseCore() => _mediaPlayer?.Pause();

    private void SeekCore(double seconds) =>
        _mediaPlayer?.PlaybackSession?.Position = TimeSpan.FromSeconds(seconds);

    private void StopPlaybackCore()
    {
        if (_mediaPlayer != null)
        {
            UnsubscribeSessionEvents();
            _mediaPlayer.Pause();
            _mediaPlayer.Source = null;
        }
    }

    private void ApplyPlaybackRateCore(double rate)
    {
        if (_mediaPlayer?.PlaybackSession == null || rate <= 0)
        {
            return;
        }

        try
        {
            _mediaPlayer.PlaybackSession.PlaybackRate = rate;
        } catch { }
    }

    private void SubscribeSessionEvents()
    {
        if (!_sessionSubscribed && _mediaPlayer?.PlaybackSession != null)
        {
            _mediaPlayer.PlaybackSession.PositionChanged -= OnPlayPositionChanged;
            _mediaPlayer.PlaybackSession.PositionChanged += OnPlayPositionChanged;
            _sessionSubscribed = true;
        }
    }

    private void UnsubscribeSessionEvents()
    {
        if (_sessionSubscribed && _mediaPlayer?.PlaybackSession != null)
        {
            _mediaPlayer.PlaybackSession.PositionChanged -= OnPlayPositionChanged;
            _sessionSubscribed = false;
        }
    }

    private void OnPlayPositionChanged(MediaPlaybackSession sender, object args)
    {
        var seconds = sender.Position.TotalSeconds;
        if (Math.Abs(seconds - _lastUiPosition) >= 0.1)
        {
            DispatcherQueue?.TryEnqueue(() =>
            {
                _lastUiPosition = seconds;
                Position = TimeSpan.FromSeconds(seconds);
            });
        }
    }

    private void OnPlayerCurrentStateChanged(MediaPlayer sender, object args)
    {
        if (_isDisposed) return;
        var playing = sender.PlaybackSession?.PlaybackState == MediaPlaybackState.Playing;
        DispatcherQueue?.TryEnqueue(() => IsPlaying = playing);
    }

    private void OnPlayerMediaOpened(MediaPlayer sender, object args)
    {
        SubscribeSessionEvents();
        sender.PlaybackSession.Position = TimeSpan.Zero;
        DispatcherQueue?.TryEnqueue(() => ApplyPlaybackRateCore(PlaybackRate));
    }

    private void OnPlayerMediaEnded(MediaPlayer sender, object args)
    {
        DispatcherQueue?.TryEnqueue(() =>
        {
            if (IsRepeatEnabled)
            {
                Seek(TimeSpan.Zero);
                PlayCore();
                return;
            }

            _mediaEnded = true;
            Position = Duration;
            IsPlaying = false;
            MediaEnded?.Invoke(this, EventArgs.Empty);
        });
    }
}
