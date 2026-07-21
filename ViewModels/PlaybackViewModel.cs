using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Zubrilka.Data;
using Zubrilka.Models;
using Zubrilka.Services;

namespace Zubrilka.ViewModels;

/// <summary>
/// Drives the playback screen: one pane per selected language, and an interruptible loop that
/// for each card repeats "show text → speak → pause" over the languages, then moves on.
/// Cards are shuffled once on start and then cycled in that order until the user exits.
/// </summary>
public partial class PlaybackViewModel : ObservableObject
{
    private readonly ITtsService _tts;
    private readonly ILanguageCatalog _catalog;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ICardRepository _cardRepository;

    public PlaybackViewModel(
        ITtsService tts,
        ILanguageCatalog catalog,
        ISettingsRepository settingsRepository,
        ICardRepository cardRepository)
    {
        _tts = tts;
        _catalog = catalog;
        _settingsRepository = settingsRepository;
        _cardRepository = cardRepository;
    }

    private Block _block = null!;
    private List<Card> _cards = new();

    // Global settings snapshot taken at start.
    private int _speedPercent = 100;
    private double _pauseSeconds = 1;

    // Exit cancels the whole loop; the pause pieces interrupt just the current phrase.
    private CancellationTokenSource _exitCts = new();
    private CancellationTokenSource _pauseCts = new();
    private volatile TaskCompletionSource<bool>? _pauseGate;
    private Task? _loopTask;

    // One pane per selected language, in playback order.
    public ObservableCollection<LanguagePane> Panes { get; } = new();

    // Header (block name).
    public string Title => _block?.Name ?? string.Empty;

    // Font size for phrase text; used as the maximum for auto-shrink.
    [ObservableProperty]
    private double _fontSize = 28;

    [ObservableProperty]
    private bool _isPaused;

    // Keeps the Pause/Resume button label in sync with IsPaused.
    public string PauseButtonText => IsPaused ? "Resume" : "Pause";
    partial void OnIsPausedChanged(bool value) => OnPropertyChanged(nameof(PauseButtonText));

    // Raised when the user exits, asking the View to leave the playback page.
    public event Action? ExitRequested;

    /// <summary>Supplies the block to play. Cards are loaded lazily in <see cref="StartAsync"/>.</summary>
    public void Initialize(Block block)
    {
        _block = block;
        OnPropertyChanged(nameof(Title));
    }

    /// <summary>Prepares panes/settings, warns about missing voices, then starts the loop.</summary>
    public async Task StartAsync()
    {
        await _catalog.EnsureLoadedAsync();
        var ttsReady = await _tts.InitializeAsync();

        // Load global settings (font/speed/pause).
        var settings = await _settingsRepository.GetAsync();
        FontSize = settings.FontSize;
        _speedPercent = settings.PlaybackSpeedPercent;
        _pauseSeconds = settings.PauseSeconds;

        // Build one pane per selected language and note any without an installed voice.
        Panes.Clear();
        var missing = new List<string>();
        foreach (var code in _block.SelectedLanguages)
        {
            var info = _catalog.Resolve(code);
            Panes.Add(new LanguagePane(code, info.Locale, info.Rtl, FontSize));
            if (ttsReady && !_tts.IsLanguageAvailable(info.Locale))
                missing.Add($"{code} ({info.Locale})");
        }

        // Guide the user if speech won't work — otherwise it fails silently.
        if (!ttsReady)
        {
            await Shell.Current.DisplayAlertAsync(
                "Text-to-speech unavailable",
                "No TTS engine is available on this device. Text will still be shown.",
                "OK");
        }
        else if (missing.Count > 0)
        {
            await Shell.Current.DisplayAlertAsync(
                "Some voices are missing",
                $"No installed voice for: {string.Join(", ", missing)}.\n\n" +
                "Install them in Android Settings → System → Languages & input → " +
                "Text-to-speech output. Those languages will show text but stay silent.",
                "OK");
        }

        // Load the block's cards and shuffle them once for this session.
        _cards = await _cardRepository.GetByBlockIdAsync(_block.Id);
        Shuffle(_cards);

        // Kick off the loop; it runs until Exit cancels the token.
        _loopTask = RunAsync(_exitCts.Token);
    }

    /// <summary>Stops the loop and any active speech. Safe to call more than once.</summary>
    public async Task StopAsync()
    {
        if (!_exitCts.IsCancellationRequested)
            _exitCts.Cancel();
        _tts.Stop();

        // Release a pause wait so the loop can observe cancellation and finish.
        _pauseGate?.TrySetResult(true);

        if (_loopTask is not null)
        {
            try { await _loopTask; }
            catch (OperationCanceledException) { /* expected */ }
            _loopTask = null;
        }
    }

    [RelayCommand]
    private void TogglePause()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    [RelayCommand]
    private void Exit() => ExitRequested?.Invoke();

    // --- Pause/resume plumbing ---

    private void Pause()
    {
        IsPaused = true;
        // Open a gate the loop will wait on, then interrupt the current utterance.
        _pauseGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pauseCts.Cancel();
    }

    private void Resume()
    {
        // Fresh token for subsequent speaks BEFORE releasing the gate.
        _pauseCts = new CancellationTokenSource();
        IsPaused = false;
        var gate = _pauseGate;
        _pauseGate = null;
        gate?.TrySetResult(true);
    }

    private Task WaitWhilePausedAsync(CancellationToken exitToken)
    {
        var gate = _pauseGate;
        return gate is null ? Task.CompletedTask : gate.Task.WaitAsync(exitToken);
    }

    // --- The playback loop ---

    private async Task RunAsync(CancellationToken exitToken)
    {
        try
        {
            // Cycle through the shuffled cards repeatedly until the user exits.
            while (!exitToken.IsCancellationRequested && _cards.Count > 0)
            {
                foreach (var card in _cards)
                {
                    RunOnUi(ClearPanes); // blank screen between cards

                    for (int repeat = 0; repeat < _block.RepeatCount; repeat++)
                    {
                        foreach (var pane in Panes)
                        {
                            exitToken.ThrowIfCancellationRequested();
                            var text = card.GetText(pane.Code) ?? string.Empty;
                            await SpeakStepAsync(pane, text, exitToken);
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal exit — nothing to do.
        }
    }

    // Shows the phrase, speaks it (re-speaking from the start if paused), then pauses.
    private async Task SpeakStepAsync(LanguagePane pane, string text, CancellationToken exitToken)
    {
        RunOnUi(() =>
        {
            pane.Text = text;
            pane.IsActive = true;
        });

        while (true)
        {
            await WaitWhilePausedAsync(exitToken); // hold here while paused

            // Cancel this utterance on either exit or pause.
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(exitToken, _pauseCts.Token);
            try
            {
                await _tts.SpeakAsync(text, pane.Locale, _speedPercent, linked.Token);
                break; // finished speaking this phrase
            }
            catch (OperationCanceledException)
            {
                exitToken.ThrowIfCancellationRequested(); // exiting: bubble up and end the loop
                // Otherwise it was a pause: loop back, wait at the gate, then re-speak.
            }
        }

        RunOnUi(() => pane.IsActive = false);

        // Pause between phrases (also pausable/interruptible).
        await DelayAsync(_pauseSeconds, exitToken);
    }

    // A delay that honours pause (holds) and exit (throws).
    private async Task DelayAsync(double seconds, CancellationToken exitToken)
    {
        var remaining = TimeSpan.FromSeconds(seconds);
        var step = TimeSpan.FromMilliseconds(100);
        while (remaining > TimeSpan.Zero)
        {
            exitToken.ThrowIfCancellationRequested();
            await WaitWhilePausedAsync(exitToken);
            var slice = remaining < step ? remaining : step;
            await Task.Delay(slice, exitToken);
            remaining -= slice;
        }
    }

    private void ClearPanes()
    {
        foreach (var pane in Panes)
        {
            pane.Text = string.Empty;
            pane.IsActive = false;
        }
    }

    // Fisher–Yates shuffle in place.
    private static void Shuffle(List<Card> cards)
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
    }

    // Runs a UI mutation on the main thread (loop continuations may be off it).
    private static void RunOnUi(Action action)
    {
        if (MainThread.IsMainThread) action();
        else MainThread.BeginInvokeOnMainThread(action);
    }
}
