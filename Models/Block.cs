using System.Text.Json;
using SQLite;

namespace Zubrilka.Models;

/// <summary>
/// A named set of flashcards imported from a single file.
/// One imported file (of any size) becomes exactly one block.
/// Also stores the block's remembered playback settings.
/// </summary>
[Table("Blocks")]
public class Block
{
    // Local primary key, assigned by SQLite on insert.
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // Display name shown on the start screen. Defaults to the imported file name; editable.
    public string Name { get; set; } = string.Empty;

    // JSON array of all language names in table (column) order, e.g. ["Hebrew","Russian"].
    public string LanguagesJson { get; set; } = "[]";

    // JSON array of the languages chosen for playback, in the user's preferred order.
    // A subset of Languages; order may differ from the table order.
    public string SelectedLanguagesJson { get; set; } = "[]";

    // How many times each card is repeated during playback. Default is 2 (see spec).
    public int RepeatCount { get; set; } = 2;

    // When the block was imported. Handy as a stable tiebreaker; listing sorts by Name.
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>All languages of this block, in table (column) order. Backed by <see cref="LanguagesJson"/>.</summary>
    [Ignore]
    public List<string> Languages
    {
        get => Deserialize(LanguagesJson);
        set => LanguagesJson = JsonSerializer.Serialize(value);
    }

    /// <summary>Languages selected for playback, in the chosen order. Backed by <see cref="SelectedLanguagesJson"/>.</summary>
    [Ignore]
    public List<string> SelectedLanguages
    {
        get => Deserialize(SelectedLanguagesJson);
        set => SelectedLanguagesJson = JsonSerializer.Serialize(value);
    }

    // Cards are loaded separately via the repository (sqlite-net has no auto relationships),
    // so this in-memory list is not persisted.
    [Ignore]
    public List<Card> Cards { get; set; } = new();

    // Shared helper: safely turn a JSON array string into a list of strings.
    private static List<string> Deserialize(string json)
        => string.IsNullOrWhiteSpace(json)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();

    /// <summary>
    /// Resets playback selection to the default: every language in table order, 2 repeats.
    /// Called right after import so a new block is immediately playable.
    /// </summary>
    public void ApplyDefaultPlaybackSettings()
    {
        SelectedLanguages = new List<string>(Languages); // all languages, original order
        RepeatCount = 2;                                 // spec default
    }
}
