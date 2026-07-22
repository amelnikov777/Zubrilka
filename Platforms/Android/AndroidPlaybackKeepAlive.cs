#if ANDROID
using Android.Content;
using Zubrilka.Services;

namespace Zubrilka.Platforms.Android;

/// <inheritdoc cref="IPlaybackKeepAlive"/>
public class AndroidPlaybackKeepAlive : IPlaybackKeepAlive
{
    private bool _running;

    public void Start()
    {
        if (_running)
            return;

        var context = global::Android.App.Application.Context;
        var intent = new Intent(context, typeof(PlaybackForegroundService));

        // Android 8+ requires StartForegroundService; the service then calls StartForeground.
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
            context.StartForegroundService(intent);
        else
            context.StartService(intent);

        _running = true;
    }

    public void Stop()
    {
        if (!_running)
            return;

        var context = global::Android.App.Application.Context;
        context.StopService(new Intent(context, typeof(PlaybackForegroundService)));
        _running = false;
    }
}
#endif
