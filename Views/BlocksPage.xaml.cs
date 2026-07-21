using Zubrilka.ViewModels;

namespace Zubrilka.Views;

public partial class BlocksPage : ContentPage
{
    private readonly BlocksViewModel _viewModel;

    public BlocksPage(BlocksViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;

        // When a block is tapped, present its switch-box as a modal page.
        _viewModel.SetupRequested += OnSetupRequested;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Reload each time the screen is shown (e.g. after returning from playback).
        _viewModel.LoadCommand.Execute(null);
    }

    private async void OnSetupRequested(BlockSetupViewModel setupViewModel)
    {
        await Navigation.PushModalAsync(new BlockSetupPage(setupViewModel));
    }

    // [Phase 5] Font settings screen — stubbed until then.
    private async void OnFontClicked(object? sender, EventArgs e)
        => await DisplayAlertAsync("Font size", "Font settings arrive in Phase 5.", "OK");

    // [Phase 5] Speed/pauses settings screen — stubbed until then.
    private async void OnSpeedClicked(object? sender, EventArgs e)
        => await DisplayAlertAsync("Speed & pauses", "Speed/pause settings arrive in Phase 5.", "OK");
}
