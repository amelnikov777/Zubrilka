using SQLite;

namespace Zubrilka.Models;

/// <summary>
/// Global, app-wide settings. Persisted as a single row (see <see cref="SingletonId"/>).
/// Per-block settings live on <see cref="Block"/> instead.
/// </summary>
[Table("AppSettings")]
public class AppSettings
{
    // There is only ever one settings row; we always read/write this fixed id.
    public const int SingletonId = 1;

    // Fixed primary key (not auto-increment) so we can upsert the single row deterministically.
    [PrimaryKey]
    public int Id { get; set; } = SingletonId;

    // Font size (device-independent units) for phrase text on the playback screen.
    public double FontSize { get; set; } = 28;

    // Speech rate as a percentage; 100 = the device's normal TTS rate. Adjusted in ±5% steps.
    public int PlaybackSpeedPercent { get; set; } = 100;

    // Pause after each spoken phrase, in seconds. Adjusted in ±1s steps.
    public double PauseSeconds { get; set; } = 1;

    // Whether we already offered to exempt the app from battery optimisation.
    // Keeps the prompt to a single appearance instead of every playback.
    // (sqlite-net adds this column automatically to an existing database.)
    public bool BatteryPromptShown { get; set; }
}
