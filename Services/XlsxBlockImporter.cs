using ClosedXML.Excel;
using Zubrilka.Models;

namespace Zubrilka.Services;

/// <summary>
/// Imports a <see cref="Block"/> from an .xlsx file using ClosedXML.
///
/// Expected layout (matches the user's files):
///   - Row 1 holds language codes, one per column (e.g. He, Ru, En, Ge, Fr, Bu).
///   - Each following row is one card: a phrase translated across those columns.
/// Empty header columns are ignored; fully empty rows are skipped; per-cell blanks
/// simply mean that card has no translation for that language.
/// </summary>
public class XlsxBlockImporter : IBlockImporter
{
    public async Task<Block> ImportAsync(Stream stream, string suggestedName, CancellationToken cancellationToken = default)
    {
        // ClosedXML needs a seekable stream, but an Android content stream may not be one.
        // Buffer into memory first (flashcard files are small). Also decouples us from the caller's stream.
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        // Parsing itself is CPU-bound and synchronous; run it off the UI thread.
        return await Task.Run(() => Parse(buffer, suggestedName), cancellationToken);
    }

    // Core parse, kept synchronous and free of MAUI types so it is easy to unit-test.
    private static Block Parse(Stream seekableStream, string suggestedName)
    {
        using var workbook = new XLWorkbook(seekableStream);

        // The app uses a single sheet per file; take the first one.
        var worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidDataException("The file has no worksheets.");

        // Work within the actually populated range so leading blank rows/columns don't matter.
        var used = worksheet.RangeUsed();
        if (used is null)
            throw new InvalidDataException("The sheet is empty.");

        int firstRow = used.RangeAddress.FirstAddress.RowNumber;
        int lastRow = used.RangeAddress.LastAddress.RowNumber;
        int firstCol = used.RangeAddress.FirstAddress.ColumnNumber;
        int lastCol = used.RangeAddress.LastAddress.ColumnNumber;

        // --- Header row: map each non-empty header cell to its column number. ---
        // Order matters: it becomes the block's language order.
        var languageColumns = new List<(int Column, string Language)>();
        var languages = new List<string>();
        for (int col = firstCol; col <= lastCol; col++)
        {
            var header = worksheet.Cell(firstRow, col).GetString().Trim();
            if (header.Length == 0)
                continue; // skip empty header columns
            languageColumns.Add((col, header));
            languages.Add(header);
        }

        if (languageColumns.Count == 0)
            throw new InvalidDataException("The first row has no language columns.");

        // --- Data rows: one card each, skipping rows with no content at all. ---
        var cards = new List<Card>();
        int orderIndex = 0;
        for (int row = firstRow + 1; row <= lastRow; row++)
        {
            var translations = new Dictionary<string, string>();
            foreach (var (col, language) in languageColumns)
            {
                var text = worksheet.Cell(row, col).GetString().Trim();
                if (text.Length > 0)
                    translations[language] = text;
            }

            // A row with every language blank carries no phrase; ignore it.
            if (translations.Count == 0)
                continue;

            cards.Add(new Card
            {
                OrderIndex = orderIndex++,
                Translations = translations,
            });
        }

        // --- Assemble the block with sensible defaults. ---
        var block = new Block
        {
            Name = string.IsNullOrWhiteSpace(suggestedName) ? "Imported block" : suggestedName.Trim(),
            Languages = languages,
            Cards = cards,
        };
        // Default playback: all languages in table order, 2 repeats (see spec).
        block.ApplyDefaultPlaybackSettings();

        return block;
    }
}
