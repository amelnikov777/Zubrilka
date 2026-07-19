using Zubrilka.Models;

namespace Zubrilka.Data;

/// <summary>Data access for <see cref="Card"/> rows.</summary>
public interface ICardRepository
{
    // All cards of a block, in their stored order (OrderIndex).
    Task<List<Card>> GetByBlockIdAsync(int blockId);

    // Number of cards in a block (for showing counts on the start screen).
    Task<int> CountByBlockIdAsync(int blockId);

    // Bulk insert, used by the importer after building a block's cards.
    Task InsertAllAsync(IEnumerable<Card> cards);

    // Remove every card of a block (used when deleting or re-importing a block).
    Task DeleteByBlockIdAsync(int blockId);
}

/// <inheritdoc cref="ICardRepository"/>
public class CardRepository : ICardRepository
{
    private readonly AppDatabase _database;

    // AppDatabase is injected so all repositories share one initialized connection.
    public CardRepository(AppDatabase database) => _database = database;

    public async Task<List<Card>> GetByBlockIdAsync(int blockId)
    {
        var connection = await _database.GetConnectionAsync();
        // Order by OrderIndex so cards come back in their original imported sequence.
        return await connection.Table<Card>()
            .Where(card => card.BlockId == blockId)
            .OrderBy(card => card.OrderIndex)
            .ToListAsync();
    }

    public async Task<int> CountByBlockIdAsync(int blockId)
    {
        var connection = await _database.GetConnectionAsync();
        return await connection.Table<Card>()
            .Where(card => card.BlockId == blockId)
            .CountAsync();
    }

    public async Task InsertAllAsync(IEnumerable<Card> cards)
    {
        var connection = await _database.GetConnectionAsync();
        // InsertAllAsync runs inside a single transaction for speed on large imports.
        await connection.InsertAllAsync(cards);
    }

    public async Task DeleteByBlockIdAsync(int blockId)
    {
        var connection = await _database.GetConnectionAsync();
        // Parameterized delete; avoids loading rows just to remove them.
        await connection.ExecuteAsync("DELETE FROM Cards WHERE BlockId = ?", blockId);
    }
}
