using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Zubrilka.Data;
using Zubrilka.Services;

namespace Zubrilka.ViewModels;

/// <summary>
/// Backs the start screen: the alphabetical list of blocks plus import/delete/open actions.
/// </summary>
public partial class BlocksViewModel : ObservableObject
{
    private readonly IBlockRepository _blockRepository;
    private readonly ICardRepository _cardRepository;
    private readonly IBlockImporter _importer;

    public BlocksViewModel(
        IBlockRepository blockRepository,
        ICardRepository cardRepository,
        IBlockImporter importer)
    {
        _blockRepository = blockRepository;
        _cardRepository = cardRepository;
        _importer = importer;
    }

    // The rows shown in the list. Rebuilt on each load; already sorted by the repository.
    public ObservableCollection<BlockListItem> Blocks { get; } = new();

    // True while a long-running action runs; used to show a spinner and block re-entry.
    [ObservableProperty]
    private bool _isBusy;

    // Raised when the user taps a block, asking the View to present the switch-box for it.
    public event Action<BlockSetupViewModel>? SetupRequested;

    /// <summary>Loads all blocks with their card counts and refreshes the list.</summary>
    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            await ReloadBlocksAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Rebuilds the list from the database. The caller owns the <see cref="IsBusy"/> guard,
    /// so callers that are already busy (import) can refresh without being turned away.
    /// </summary>
    private async Task ReloadBlocksAsync()
    {
        var blocks = await _blockRepository.GetAllAsync(); // already sorted by name
        Blocks.Clear();
        foreach (var block in blocks)
        {
            var count = await _cardRepository.CountByBlockIdAsync(block.Id);
            // Pass the shared commands so each row can bind tap/long-press directly.
            Blocks.Add(new BlockListItem(block, count, OpenSetupCommand, DeleteCommand));
        }
    }

    /// <summary>Picks an .xlsx file, imports it into a new block, saves it, and reloads.</summary>
    [RelayCommand]
    private async Task ImportAsync()
    {
        if (IsBusy) return;

        // Restrict the picker to Excel files per platform (Android uses the MIME type).
        var xlsxType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            [DevicePlatform.Android] = new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            [DevicePlatform.WinUI] = new[] { ".xlsx" },
        });

        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Select an .xlsx flashcard file",
            FileTypes = xlsxType,
        });

        if (result is null)
            return; // user cancelled

        IsBusy = true;
        try
        {
            // Block name defaults to the file name without extension (user can rename later).
            var suggestedName = Path.GetFileNameWithoutExtension(result.FileName);

            await using var stream = await result.OpenReadAsync();
            var block = await _importer.ImportAsync(stream, suggestedName);

            if (block.Cards.Count == 0)
            {
                await ShowAlertAsync("Nothing to import", "The file has no phrase rows.");
                return;
            }

            await _blockRepository.SaveBlockWithCardsAsync(block);
            await ReloadBlocksAsync(); // we already hold IsBusy; LoadAsync would no-op
        }
        catch (Exception ex)
        {
            // Import can fail on malformed files; show a friendly message instead of crashing.
            await ShowAlertAsync("Import failed", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Long-press action: confirm, then delete the block and its cards.</summary>
    [RelayCommand]
    private async Task DeleteAsync(BlockListItem? item)
    {
        if (item is null) return;

        var confirmed = await ConfirmAsync(
            "Delete block",
            $"Delete \"{item.Name}\" and all its cards?",
            accept: "Delete",
            cancel: "Cancel");
        if (!confirmed)
            return;

        await _blockRepository.DeleteAsync(item.Block);
        Blocks.Remove(item);
    }

    // [FUTURE] Card editing: a screen to rename a block and add/edit/delete its cards.
    // Card and Block already carry everything needed; it would save through
    // IBlockRepository.SaveBlockWithCardsAsync, the same path import uses.

    /// <summary>Tap action: open the switch-box (playback setup) for the block.</summary>
    [RelayCommand]
    private void OpenSetup(BlockListItem? item)
    {
        if (item is null) return;

        // Build the setup view-model here (we hold the repository) and let the View present it.
        var setupViewModel = new BlockSetupViewModel(_blockRepository, item.Block);
        SetupRequested?.Invoke(setupViewModel);
    }

    // --- Small UI helpers (kept here so commands stay readable). ---

    private static Task ShowAlertAsync(string title, string message)
        => Shell.Current.DisplayAlertAsync(title, message, "OK");

    private static Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
        => Shell.Current.DisplayAlertAsync(title, message, accept, cancel);
}
