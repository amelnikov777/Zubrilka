using Zubrilka.Models;

namespace Zubrilka.Services;

/// <summary>
/// Turns an external source (a spreadsheet file today) into an in-memory <see cref="Block"/>.
/// The returned block is NOT yet saved: its Id is 0 and its cards live in <see cref="Block.Cards"/>.
/// Persisting is a separate step (see IBlockRepository.SaveBlockWithCardsAsync), so the same
/// abstraction can back different sources.
/// </summary>
public interface IBlockImporter
{
    /// <summary>
    /// Parses a source stream into a block.
    /// </summary>
    /// <param name="stream">The raw file contents (e.g. from the Android file picker).</param>
    /// <param name="suggestedName">
    /// Proposed block name, typically the file name without extension; the user may edit it later.
    /// </param>
    /// <param name="cancellationToken">Cancels a long import.</param>
    Task<Block> ImportAsync(Stream stream, string suggestedName, CancellationToken cancellationToken = default);
}

// [FUTURE] A GoogleSheetsBlockImporter : IBlockImporter will fetch a sheet and return the
// same Block type, so the rest of the app (saving, playback) stays unchanged.

// [FUTURE] An IBlockExporter with an XlsxBlockExporter will write a Block back out to .xlsx
// (row 1 = language codes, one row per card), mirroring the import format above.
