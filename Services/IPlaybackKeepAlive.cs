namespace Zubrilka.Services;

/// <summary>
/// Keeps playback running when the app is not in the foreground (screen switched off with the
/// power button, or the user moved to another app). On Android this is a foreground service
/// with an ongoing notification plus a partial wake lock; without it the system suspends the
/// process and speech stops.
/// </summary>
public interface IPlaybackKeepAlive
{
    /// <summary>Begins keeping the app alive. Safe to call when already started.</summary>
    void Start();

    /// <summary>Stops keeping the app alive.</summary>
    void Stop();
}
