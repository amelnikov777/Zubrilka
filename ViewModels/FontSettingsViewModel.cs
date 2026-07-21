using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Zubrilka.Data;
using Zubrilka.Models;

namespace Zubrilka.ViewModels;

/// <summary>
/// Backs the font-size settings screen: a sample phrase shown at the chosen size,
/// with larger/smaller buttons. Changes are saved immediately.
/// </summary>
public partial class FontSettingsViewModel : ObservableObject
{
    // Keep the phrase size within readable bounds.
    private const double MinFontSize = 12;
    private const double MaxFontSize = 96;
    private const double Step = 2;

    private readonly ISettingsRepository _settingsRepository;
    private AppSettings? _settings;

    public FontSettingsViewModel(ISettingsRepository settingsRepository)
        => _settingsRepository = settingsRepository;

    /// <summary>Example phrase rendered at the current size so the user can judge it.</summary>
    public string SampleText => "The quick brown fox jumps over the lazy dog.";

    [ObservableProperty]
    private double _fontSize = 28;

    /// <summary>Loads the stored size when the screen opens.</summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        _settings = await _settingsRepository.GetAsync();
        FontSize = _settings.FontSize;
    }

    [RelayCommand]
    private Task IncreaseAsync() => SetSizeAsync(FontSize + Step);

    [RelayCommand]
    private Task DecreaseAsync() => SetSizeAsync(FontSize - Step);

    // Clamps, applies, and persists the new size.
    private async Task SetSizeAsync(double newSize)
    {
        var clamped = Math.Clamp(newSize, MinFontSize, MaxFontSize);
        if (Math.Abs(clamped - FontSize) < 0.01)
            return; // already at the limit

        FontSize = clamped;

        if (_settings is not null)
        {
            _settings.FontSize = clamped;
            await _settingsRepository.SaveAsync(_settings);
        }
    }
}
