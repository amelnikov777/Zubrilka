using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zubrilka.Services;

/// <summary>Resolved facts about one language: how to speak it and how to show it.</summary>
public record LanguageInfo(string Code, string Name, string Locale, bool Rtl, bool IsKnown);

/// <summary>
/// Maps a block's language header (e.g. "he" or "he-IL") to a TTS locale, display name,
/// and text direction, using the bundled <c>Resources/Raw/languages.json</c> file.
/// </summary>
public interface ILanguageCatalog
{
    /// <summary>Loads the reference file once (safe to call repeatedly).</summary>
    Task EnsureLoadedAsync();

    /// <summary>
    /// Resolves a header to language facts. Never returns null: unknown headers fall back to
    /// using the header itself as the locale (IsKnown = false) so TTS can still attempt it.
    /// </summary>
    LanguageInfo Resolve(string header);
}

/// <inheritdoc cref="ILanguageCatalog"/>
public class LanguageCatalog : ILanguageCatalog
{
    private const string ResourceFileName = "languages.json";

    // Loaded map keyed by lower-cased language code (e.g. "he").
    private Dictionary<string, LanguageInfo>? _byCode;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    public async Task EnsureLoadedAsync()
    {
        if (_byCode is not null)
            return;

        await _loadLock.WaitAsync();
        try
        {
            if (_byCode is not null)
                return;

            // Read the bundled raw asset from the app package.
            await using var stream = await FileSystem.OpenAppPackageFileAsync(ResourceFileName);
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            var file = JsonSerializer.Deserialize<LanguagesFile>(json) ?? new LanguagesFile();
            var map = new Dictionary<string, LanguageInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var (code, entry) in file.Languages)
            {
                map[code] = new LanguageInfo(
                    Code: code,
                    Name: entry.Name ?? code,
                    Locale: entry.Locale ?? code,
                    Rtl: entry.Rtl,
                    IsKnown: true);
            }

            _byCode = map;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public LanguageInfo Resolve(string header)
    {
        var map = _byCode ?? throw new InvalidOperationException("Call EnsureLoadedAsync() first.");
        var key = header.Trim();

        // 1) Exact code match, e.g. "he" or "HE" (dictionary is case-insensitive).
        if (map.TryGetValue(key, out var info))
            return info;

        // 2) Full locale like "he-IL": match the language part before the dash.
        var dash = key.IndexOf('-');
        if (dash > 0 && map.TryGetValue(key[..dash], out info))
            // Keep the caller's specific locale (region) but reuse the known name/direction.
            return info with { Code = key, Locale = key };

        // 3) A locale value that matches an entry's locale exactly (e.g. someone used "en-US").
        foreach (var entry in map.Values)
            if (string.Equals(entry.Locale, key, StringComparison.OrdinalIgnoreCase))
                return entry;

        // 4) Unknown: fall back to using the header itself as the locale.
        return new LanguageInfo(Code: key, Name: key, Locale: key, Rtl: false, IsKnown: false);
    }

    // --- JSON shapes matching Resources/Raw/languages.json ---

    private sealed class LanguagesFile
    {
        [JsonPropertyName("languages")]
        public Dictionary<string, LanguageEntry> Languages { get; set; } = new();
    }

    private sealed class LanguageEntry
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("locale")] public string? Locale { get; set; }
        [JsonPropertyName("rtl")] public bool Rtl { get; set; }
    }
}
