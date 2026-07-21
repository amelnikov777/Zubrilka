using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Zubrilka.ViewModels;

/// <summary>
/// One selectable language row in the switch-box: its code and whether it's checked.
/// The row's position in the list is its playback order. Carries the reorder commands
/// (owned by the setup view-model) so the row template binds them with typed bindings.
/// </summary>
public partial class LanguageChoice : ObservableObject
{
    public LanguageChoice(string code, bool isSelected, ICommand moveUpCommand, ICommand moveDownCommand)
    {
        Code = code;
        IsSelected = isSelected;
        MoveUpCommand = moveUpCommand;
        MoveDownCommand = moveDownCommand;
    }

    // The language code exactly as stored on the block (e.g. "he").
    public string Code { get; }

    // Reorder commands; this item is passed as the command parameter.
    public ICommand MoveUpCommand { get; }
    public ICommand MoveDownCommand { get; }

    // Whether this language is included in playback.
    [ObservableProperty]
    private bool _isSelected;
}
