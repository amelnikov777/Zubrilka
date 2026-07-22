#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace Zubrilka.Platforms.Android;

/// <summary>
/// A foreground service that exists only to keep the app's process alive and the CPU awake
/// while playback runs, so speech continues with the screen off or the app in the background.
/// It does not play audio itself — the playback loop and TextToSpeech stay in the app.
/// </summary>
[Service(Exported = false, ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeMediaPlayback)]
public class PlaybackForegroundService : Service
{
    private const string ChannelId = "zubrilka_playback";
    private const int NotificationId = 1001;

    private PowerManager.WakeLock? _wakeLock;

    // Not a bound service.
    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        CreateNotificationChannel();
        StartForeground(NotificationId, BuildNotification());
        AcquireWakeLock();

        // Don't recreate the service if the system kills it; playback is user-driven.
        return StartCommandResult.NotSticky;
    }

    public override void OnDestroy()
    {
        ReleaseWakeLock();
        base.OnDestroy();
    }

    // Android 8+ requires a channel for any notification.
    private void CreateNotificationChannel()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
            return;

        var channel = new NotificationChannel(
            ChannelId,
            "Playback",
            NotificationImportance.Low) // low: no sound, stays quiet in the shade
        {
            Description = "Keeps phrase playback running in the background.",
        };

        var manager = (NotificationManager?)GetSystemService(NotificationService);
        manager?.CreateNotificationChannel(channel);
    }

    private Notification BuildNotification()
    {
        // Tapping the notification brings the app back to the front.
        var launchIntent = new Intent(this, typeof(global::Zubrilka.MainActivity));
        launchIntent.SetFlags(ActivityFlags.SingleTop);

        // Immutable is required from API 31 on and only exists from API 23.
        var pendingFlags = PendingIntentFlags.UpdateCurrent;
        if (OperatingSystem.IsAndroidVersionAtLeast(23))
            pendingFlags |= PendingIntentFlags.Immutable;

        var contentIntent = PendingIntent.GetActivity(this, 0, launchIntent, pendingFlags);

        // Calls are statements rather than a fluent chain: each Set* returns a nullable
        // builder in the bindings, which would trip nullable analysis when chained.
        var builder = new NotificationCompat.Builder(this, ChannelId);
        builder.SetContentTitle("Zubrilka");
        builder.SetContentText("Playing phrases");
        builder.SetSmallIcon(global::Android.Resource.Drawable.IcMediaPlay);
        builder.SetContentIntent(contentIntent);
        builder.SetOngoing(true);   // user can't swipe it away while playing
        builder.SetShowWhen(false);

        // Build() is annotated nullable in the bindings but never returns null in practice.
        return builder.Build()!;
    }

    // A partial wake lock keeps the CPU running once the screen goes off.
    private void AcquireWakeLock()
    {
        if (_wakeLock is not null)
            return;

        var power = (PowerManager?)GetSystemService(PowerService);
        _wakeLock = power?.NewWakeLock(WakeLockFlags.Partial, "Zubrilka::Playback");
        _wakeLock?.Acquire();
    }

    private void ReleaseWakeLock()
    {
        if (_wakeLock is { IsHeld: true })
            _wakeLock.Release();
        _wakeLock?.Dispose();
        _wakeLock = null;
    }
}
#endif
