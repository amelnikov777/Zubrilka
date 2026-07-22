#if ANDROID
using Android.Content;
using Android.OS;
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

    public bool IsBackgroundUnrestricted
    {
        get
        {
            // Battery optimisation only exists from API 23; older systems don't restrict us.
            if (!OperatingSystem.IsAndroidVersionAtLeast(23))
                return true;

            var context = global::Android.App.Application.Context;
            var power = (PowerManager?)context.GetSystemService(Context.PowerService);
            // If we can't tell, assume unrestricted rather than nagging the user.
            return power?.IsIgnoringBatteryOptimizations(context.PackageName!) ?? true;
        }
    }

    public void RequestBackgroundUnrestricted()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(23))
            return;

        var context = global::Android.App.Application.Context;

        // Opens the system "allow app to run in background" confirmation.
        var intent = new Intent(global::Android.Provider.Settings.ActionRequestIgnoreBatteryOptimizations);
        intent.SetData(global::Android.Net.Uri.Parse("package:" + context.PackageName));
        intent.SetFlags(ActivityFlags.NewTask); // started from a non-activity context
        context.StartActivity(intent);
    }
}
#endif
