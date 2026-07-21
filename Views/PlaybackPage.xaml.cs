using Zubrilka.Models;
using Zubrilka.ViewModels;

namespace Zubrilka.Views;

public partial class PlaybackPage : ContentPage
{
    private readonly PlaybackViewModel _viewModel;
    private bool _started;

    public PlaybackPage(PlaybackViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _viewModel.ExitRequested += OnExitRequested;
    }

    /// <summary>Supplies the block to play. Call before navigating to this page.</summary>
    public void Initialize(Block block) => _viewModel.Initialize(block);

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Start the loop only once, even if the page re-appears.
        if (_started) return;
        _started = true;
        await _viewModel.StartAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        // Stop speech and the loop whenever we leave the screen (Exit or hardware back).
        await _viewModel.StopAsync();
    }

    private async void OnExitRequested()
        => await Navigation.PopAsync();
}
