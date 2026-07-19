using System.Text.Json;
using SQLite;

namespace Zubrilka.Models;

/// <summary>
/// A single flashcard belonging to a <see cref="Block"/>.
/// Holds one phrase translated into several languages.
/// </summary>
[Table("Cards")]
public class Card
{
    // Local primary key, assigned by SQLite on insert.
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // Foreign key to the owning block. Indexed because we always query cards by block.
    [Indexed]
    public int BlockId { get; set; }

    // Preserves the original row order from the imported file.
    // Used to keep a stable order before playback shuffles a working copy.
    public int OrderIndex { get; set; }

    // Raw JSON persisted in the database, e.g. {"Hebrew":"...","Russian":"..."}.
    // Stored as text because SQLite has no native dictionary type.
    public string TranslationsJson { get; set; } = "{}";

    // [FUTURE] Spaced-repetition fields. Declared now so the schema is stable,
    // but not read or written by any feature yet.
    public DateTime? LastReviewedAt { get; set; }
    public int ReviewCount { get; set; }

    /// <summary>
    /// Convenience view over <see cref="TranslationsJson"/> as a language -> phrase map.
    /// [Ignore] keeps sqlite-net from trying to persist this computed property.
    /// Keys are language names exactly as they appear in the block (e.g. "Hebrew").
    /// </summary>
    [Ignore]
    public Dictionary<string, string> Translations
    {
        // Deserialize on read; fall back to an empty map if the JSON is missing/invalid.
        get => string.IsNullOrWhiteSpace(TranslationsJson)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(TranslationsJson)
              ?? new Dictionary<string, string>();
        // Serialize back into the persisted JSON column on write.
        set => TranslationsJson = JsonSerializer.Serialize(value);
    }

    /// <summary>Returns the phrase for a language, or null if this card has no such translation.</summary>
    public string? GetText(string language)
        => Translations.TryGetValue(language, out var text) ? text : null;
}
