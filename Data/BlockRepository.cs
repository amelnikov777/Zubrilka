using Zubrilka.Models;

namespace Zubrilka.Data;

/// <summary>Data access for <see cref="Block"/> rows.</summary>
public interface IBlockRepository
{
    // All blocks, sorted alphabetically by name (start-screen order).
    Task<List<Block>> GetAllAsync();

    // A single block by id, or null if it no longer exists.
    Task<Block?> GetByIdAsync(int id);

    // Insert a new block or update an existing one; returns the block's id.
    Task<int> SaveAsync(Block block);

    // Delete a block together with all of its cards.
    Task DeleteAsync(Block block);
}

/// <inheritdoc cref="IBlockRepository"/>
public class BlockRepository : IBlockRepository
{
    private readonly AppDatabase _database;
    private readonly ICardRepository _cardRepository;

    // Depends on the card repository so deleting a block can cascade to its cards.
    public BlockRepository(AppDatabase database, ICardRepository cardRepository)
    {
        _database = database;
        _cardRepository = cardRepository;
    }

    public async Task<List<Block>> GetAllAsync()
    {
        var connection = await _database.GetConnectionAsync();
        // Case-insensitive alphabetical order is applied in memory (SQLite ORDER BY is
        // case-sensitive for non-ASCII); fine for the small number of blocks we expect.
        var blocks = await connection.Table<Block>().ToListAsync();
        return blocks
            .OrderBy(block => block.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task<Block?> GetByIdAsync(int id)
    {
        var connection = await _database.GetConnectionAsync();
        // FindAsync returns null (instead of throwing) when the id is not present.
        return await connection.FindAsync<Block>(id);
    }

    public async Task<int> SaveAsync(Block block)
    {
        var connection = await _database.GetConnectionAsync();

        if (block.Id == 0)
            // New block: Insert assigns the auto-increment id back onto the object.
            await connection.InsertAsync(block);
        else
            // Existing block: overwrite its row.
            await connection.UpdateAsync(block);

        return block.Id;
    }

    public async Task DeleteAsync(Block block)
    {
        var connection = await _database.GetConnectionAsync();
        // Remove child cards first to avoid orphaned rows, then the block itself.
        await _cardRepository.DeleteByBlockIdAsync(block.Id);
        await connection.DeleteAsync(block);
    }
}
