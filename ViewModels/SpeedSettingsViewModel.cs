using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Zubrilka.Data;
using Zubrilka.Models;

namespace Zubrilka.ViewModels;

/// <summary>
/// Backs the speech-speed / pause settings screen. Speed is a percentage adjusted in ±5%
/// steps; the pause between phrases is in seconds, adjusted in ±1s steps. Saved immediately.
/// </summary>
public partial class SpeedSettingsViewModel : ObservableObject
{
    private const int MinSpeed = 25;
    private const int MaxSpeed = 300;
    private const int SpeedStep = 5;

    private const double MinPause = 0;
    private const double MaxPause = 30;
    private const double PauseStep = 1;

    private readonly ISettingsRepository _settingsRepository;
    private AppSettings? _settings;

    public SpeedSettingsViewModel(ISettingsRepository settingsRepository)
        => _settingsRepository = settingsRepository;

    [ObservableProperty]
    private int _speedPercent = 100;

    [ObservableProperty]
    private double _pauseSeconds = 1;

    // Display strings kept in sync with the values above.
    public string SpeedText => $"{SpeedPercent}%";
    public string PauseText => $"{PauseSeconds:0} s";

    partial void OnSpeedPercentChanged(int value) => OnPropertyChanged(nameof(SpeedText));
    partial void OnPauseSecondsChanged(double value) => OnPropertyChanged(nameof(PauseText));

    /// <summary>Loads stored values when the screen opens.</summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        _settings = await _settingsRepository.GetAsync();
        SpeedPercent = _settings.PlaybackSpeedPercent;
        PauseSeconds = _settings.PauseSeconds;
    }

    [RelayCommand]
    private Task IncreaseSpeedAsync() => SetSpeedAsync(SpeedPercent + SpeedStep);

    [RelayCommand]
    private Task DecreaseSpeedAsync() => SetSpeedAsync(SpeedPercent - SpeedStep);

    [RelayCommand]
    private Task IncreasePauseAsync() => SetPauseAsync(PauseSeconds + PauseStep);

    [RelayCommand]
    private Task DecreasePauseAsync() => SetPauseAsync(PauseSeconds - PauseStep);

    private async Task SetSpeedAsync(int newSpeed)
    {
        var clamped = Math.Clamp(newSpeed, MinSpeed, MaxSpeed);
        if (clamped == SpeedPercent)
            return;

        SpeedPercent = clamped;
        await PersistAsync();
    }

    private async Task SetPauseAsync(double newPause)
    {
        var clamped = Math.Clamp(newPause, MinPause, MaxPause);
        if (Math.Abs(clamped - PauseSeconds) < 0.01)
            return;

        PauseSeconds = clamped;
        await PersistAsync();
    }

    // Writes both values back in one save.
    private async Task PersistAsync()
    {
        if (_settings is null)
            return;

        _settings.PlaybackSpeedPercent = SpeedPercent;
        _settings.PauseSeconds = PauseSeconds;
        await _settingsRepository.SaveAsync(_settings);
    }
}
