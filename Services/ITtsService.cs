namespace Zubrilka.Services;

/// <summary>
/// Speaks phrases through the device's offline text-to-speech engine.
/// Implemented per platform (Android here) because we need speech-rate control,
/// which the cross-platform MAUI Essentials TextToSpeech does not expose.
/// </summary>
public interface ITtsService
{
    /// <summary>Initializes the engine. Returns false if TTS is unavailable on the device.</summary>
    Task<bool> InitializeAsync();

    /// <summary>
    /// Whether a usable voice for the given BCP-47 locale (e.g. "he-IL") is installed.
    /// Call after <see cref="InitializeAsync"/>.
    /// </summary>
    bool IsLanguageAvailable(string localeTag);

    /// <summary>
    /// Speaks <paramref name="text"/> in <paramref name="localeTag"/> at the given rate
    /// (100 = normal). Completes when the utterance finishes; cancelling stops speech
    /// immediately (used for pause/exit).
    /// </summary>
    Task SpeakAsync(string text, string localeTag, int speedPercent, CancellationToken cancellationToken);

    /// <summary>Stops any current speech right away.</summary>
    void Stop();
}
