using Microsoft.Extensions.DependencyInjection;
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

        // Resolve the playback page via DI, hand it the block, and navigate to it.
        var services = IPlatformApplication.Current!.Services;
        var page = services.GetRequiredService<PlaybackPage>();
        page.Initialize(block);
        await Shell.Current.Navigation.PushAsync(page);
    }
}
