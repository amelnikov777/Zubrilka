#if ANDROID
using System.Collections.Concurrent;
using Android.Speech.Tts;
using Java.Util;
using Zubrilka.Services;
// Disambiguate from the MAUI Essentials types pulled in by MAUI global usings.
using AndroidTts = Android.Speech.Tts.TextToSpeech;
using JavaLocale = Java.Util.Locale;

namespace Zubrilka.Platforms.Android;

/// <summary>
/// Android implementation of <see cref="ITtsService"/> over Android's native
/// <see cref="AndroidTts"/> engine. Supports speech-rate control and awaitable,
/// cancellable utterances.
/// </summary>
public class AndroidTtsService : Java.Lang.Object, ITtsService, AndroidTts.IOnInitListener
{
    private AndroidTts? _tts;
    private TaskCompletionSource<bool>? _initTcs;
    private volatile bool _ready;

    // Maps an in-flight utterance id to the task awaiting its completion.
    private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _pending = new();

    public Task<bool> InitializeAsync()
    {
        if (_ready)
            return Task.FromResult(true);

        _initTcs = new TaskCompletionSource<bool>();
        // Create the engine; OnInit fires once it is ready (or fails).
        _tts = new AndroidTts(global::Android.App.Application.Context, this);
        return _initTcs.Task;
    }

    public void OnInit(OperationResult status)
    {
        _ready = status == OperationResult.Success;
        if (_ready && _tts is not null)
            // One listener routes all utterance callbacks back to the pending tasks.
            _tts.SetOnUtteranceProgressListener(new ProgressListener(this));
        _initTcs?.TrySetResult(_ready);
    }

    public bool IsLanguageAvailable(string localeTag)
    {
        if (!_ready || _tts is null)
            return false;

        var result = _tts.IsLanguageAvailable(JavaLocale.ForLanguageTag(localeTag));
        // Treat any level of support (language / country / variant) as available.
        return result is LanguageAvailableResult.Available
            or LanguageAvailableResult.CountryAvailable
            or LanguageAvailableResult.CountryVarAvailable;
    }

    public async Task SpeakAsync(string text, string localeTag, int speedPercent, CancellationToken cancellationToken)
    {
        // No engine (or empty text): skip speaking but let the caller keep showing text.
        if (!_ready || _tts is null || string.IsNullOrWhiteSpace(text))
            return;

        cancellationToken.ThrowIfCancellationRequested();

        _tts.SetLanguage(JavaLocale.ForLanguageTag(localeTag));
        // 100% -> 1.0x; clamp to a sane range so extreme settings can't break the engine.
        _tts.SetSpeechRate(Math.Clamp(speedPercent, 10, 300) / 100f);

        var utteranceId = Guid.NewGuid().ToString("N");
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[utteranceId] = tcs;

        // Cancellation (pause/exit): stop speech now and unblock the await.
        using var registration = cancellationToken.Register(() =>
        {
            _tts?.Stop();
            if (_pending.TryRemove(utteranceId, out var pending))
                pending.TrySetCanceled();
        });

        var status = _tts.Speak(text, QueueMode.Flush, null, utteranceId);
        if (status != OperationResult.Success)
        {
            // Couldn't enqueue: don't hang waiting for a callback that won't come.
            _pending.TryRemove(utteranceId, out _);
            return;
        }

        await tcs.Task; // completes on OnDone/OnError, or throws if cancelled
    }

    public void Stop() => _tts?.Stop();

    // Called from the progress listener when an utterance finishes or errors.
    private void Complete(string? utteranceId)
    {
        if (utteranceId is not null && _pending.TryRemove(utteranceId, out var tcs))
            tcs.TrySetResult(true);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tts?.Stop();
            _tts?.Shutdown();
            _tts = null;
        }
        base.Dispose(disposing);
    }

    // Bridges Android's utterance callbacks (on binder threads) to the pending tasks.
    private sealed class ProgressListener : UtteranceProgressListener
    {
        private readonly AndroidTtsService _owner;
        public ProgressListener(AndroidTtsService owner) => _owner = owner;

        public override void OnStart(string? utteranceId) { }
        public override void OnDone(string? utteranceId) => _owner.Complete(utteranceId);

        // The (string,int) overload isn't available in this binding, so we override the
        // deprecated one; it's the only error callback we get. [Obsolete] silences CS0672.
        [Obsolete]
        public override void OnError(string? utteranceId) => _owner.Complete(utteranceId);
    }
}
#endif
