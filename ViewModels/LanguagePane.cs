using CommunityToolkit.Mvvm.ComponentModel;

namespace Zubrilka.ViewModels;

/// <summary>
/// One on-screen section during playback, dedicated to a single language.
/// The screen shows one pane per selected language, top to bottom in playback order.
/// </summary>
public partial class LanguagePane : ObservableObject
{
    public LanguagePane(string code, string locale, bool rtl, double maxFontSize)
    {
        Code = code;
        Locale = locale;
        Rtl = rtl;
        MaxFontSize = maxFontSize;
    }

    // Language code shown as a small caption (e.g. "he").
    public string Code { get; }

    // BCP-47 locale used by TTS (e.g. "he-IL").
    public string Locale { get; }

    // Right-to-left script: the pane flips text direction when true.
    public bool Rtl { get; }

    // The phrase currently shown in this pane ("" clears it between cards).
    [ObservableProperty]
    private string _text = string.Empty;

    // Highlights the pane whose phrase is being spoken right now.
    [ObservableProperty]
    private bool _isActive;

    // Maximum font size for this pane's phrase (the user's setting); auto-shrink starts here.
    [ObservableProperty]
    private double _maxFontSize = 28;
}
