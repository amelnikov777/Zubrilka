using Zubrilka.Models;
using Zubrilka.ViewModels;

namespace Zubrilka.Views;

public partial class BlockSetupPage : ContentPage
{
    private readonly BlockSetupViewModel _viewModel;

    public BlockSetupPage(BlockSetupViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        _viewModel.CloseRequested += OnCloseRequested;
        _viewModel.PlayRequested += OnPlayRequested;
    }

    private async void OnCloseRequested()
        => await Navigation.PopModalAsync();

    private async void OnPlayRequested(Block block)
    {
        // Settings are already saved by the view-model. Close the switch-box first.
        await Navigation.PopModalAsync();

        // [Phase 4] Replace this with navigation to the playback screen + TTS engine.
        var plan = string.Join(" → ", block.SelectedLanguages);
        await Shell.Current.DisplayAlertAsync(
            "Playback (Phase 4)",
            $"Will play \"{block.Name}\"\nOrder: {plan}\nRepeats: {block.RepeatCount}",
            "OK");
    }
}
