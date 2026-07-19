using Zubrilka.Models;

namespace Zubrilka.Data;

/// <summary>Reads and writes the single global <see cref="AppSettings"/> row.</summary>
public interface ISettingsRepository
{
    // Returns the current settings, creating defaults on first run.
    Task<AppSettings> GetAsync();

    // Persists changed settings.
    Task SaveAsync(AppSettings settings);
}

/// <inheritdoc cref="ISettingsRepository"/>
public class SettingsRepository : ISettingsRepository
{
    private readonly AppDatabase _database;

    public SettingsRepository(AppDatabase database) => _database = database;

    public async Task<AppSettings> GetAsync()
    {
        var connection = await _database.GetConnectionAsync();

        // Look up the one and only settings row by its fixed id.
        var settings = await connection.FindAsync<AppSettings>(AppSettings.SingletonId);
        if (settings is null)
        {
            // First launch: create the row with default values so callers always get one.
            settings = new AppSettings();
            await connection.InsertAsync(settings);
        }

        return settings;
    }

    public async Task SaveAsync(AppSettings settings)
    {
        var connection = await _database.GetConnectionAsync();
        // Fixed primary key means InsertOrReplace reliably upserts the single row.
        await connection.InsertOrReplaceAsync(settings);
    }
}
