using System.Windows.Input;
using Zubrilka.Models;

namespace Zubrilka.ViewModels;

/// <summary>
/// A read-only row shown on the start screen. Wraps a <see cref="Block"/> together with
/// derived display text (languages, card count) and the tap/long-press commands, so the
/// list template can bind everything with strongly-typed (compiled) bindings.
/// </summary>
public class BlockListItem
{
    public BlockListItem(Block block, int cardCount, ICommand openCommand, ICommand deleteCommand)
    {
        Block = block;
        CardCount = cardCount;
        OpenCommand = openCommand;
        DeleteCommand = deleteCommand;
    }

    // The underlying block, needed when opening the switch-box or deleting.
    public Block Block { get; }

    public int CardCount { get; }

    // Commands owned by the list view-model; the item is passed as the command parameter.
    public ICommand OpenCommand { get; }
    public ICommand DeleteCommand { get; }

    public string Name => Block.Name;

    // e.g. "he, ru, en" — the block's languages in table order.
    public string LanguagesText => string.Join(", ", Block.Languages);

    // e.g. "50 cards · 3 languages" for a compact subtitle.
    public string Subtitle => $"{CardCount} cards · {Block.Languages.Count} languages";
}
