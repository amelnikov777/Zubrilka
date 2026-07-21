using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Zubrilka.Data;
using Zubrilka.Models;

namespace Zubrilka.ViewModels;

/// <summary>
/// Backs the switch-box (playback setup) for one block: which languages play, in what order,
/// and how many repeats. On Play it persists these choices back onto the block.
/// </summary>
public partial class BlockSetupViewModel : ObservableObject
{
    private readonly IBlockRepository _blockRepository;
    private readonly Block _block;

    public BlockSetupViewModel(IBlockRepository blockRepository, Block block)
    {
        _blockRepository = blockRepository;
        _block = block;

        Title = block.Name;
        RepeatCount = block.RepeatCount;
        Languages = BuildLanguageList(block);
    }

    // Order the list as: selected languages first (in their saved order), then the rest.
    private ObservableCollection<LanguageChoice> BuildLanguageList(Block block)
    {
        var selected = block.SelectedLanguages;                 // saved order of chosen languages
        var selectedSet = new HashSet<string>(selected);

        var ordered = new List<LanguageChoice>();
        foreach (var code in selected)
            ordered.Add(new LanguageChoice(code, isSelected: true, MoveUpCommand, MoveDownCommand));
        // Append any block languages that aren't currently selected, keeping table order.
        foreach (var code in block.Languages)
            if (!selectedSet.Contains(code))
                ordered.Add(new LanguageChoice(code, isSelected: false, MoveUpCommand, MoveDownCommand));

        return new ObservableCollection<LanguageChoice>(ordered);
    }

    // Shown as the popup header.
    public string Title { get; }

    // Languages in playback order; checked ones are the selected subset.
    public ObservableCollection<LanguageChoice> Languages { get; }

    // Number of times each card is repeated (>= 1).
    [ObservableProperty]
    private int _repeatCount;

    // Raised when the user starts playback (after settings are saved). Carries the saved block.
    public event Action<Block>? PlayRequested;

    // Raised when the popup should just close (Cancel / back).
    public event Action? CloseRequested;

    [RelayCommand]
    private void MoveUp(LanguageChoice? item)
    {
        if (item is null) return;
        var index = Languages.IndexOf(item);
        if (index > 0)
            Languages.Move(index, index - 1);
    }

    [RelayCommand]
    private void MoveDown(LanguageChoice? item)
    {
        if (item is null) return;
        var index = Languages.IndexOf(item);
        if (index >= 0 && index < Languages.Count - 1)
            Languages.Move(index, index + 1);
    }

    [RelayCommand]
    private void IncreaseRepeat() => RepeatCount++;

    [RelayCommand]
    private void DecreaseRepeat()
    {
        if (RepeatCount > 1) // at least one pass
            RepeatCount--;
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke();

    /// <summary>Saves the selection/order/repeats to the block, then requests playback.</summary>
    [RelayCommand]
    private async Task PlayAsync()
    {
        // Selected languages, in the current on-screen order.
        var selected = Languages.Where(l => l.IsSelected).Select(l => l.Code).ToList();
        if (selected.Count == 0)
        {
            await Shell.Current.DisplayAlertAsync("No languages", "Select at least one language to play.", "OK");
            return;
        }

        // Persist the remembered per-block settings.
        _block.SelectedLanguages = selected;
        _block.RepeatCount = RepeatCount;
        await _blockRepository.SaveAsync(_block);

        PlayRequested?.Invoke(_block);
    }
}
