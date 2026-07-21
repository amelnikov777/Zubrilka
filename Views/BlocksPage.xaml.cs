using Microsoft.Extensions.DependencyInjection;
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

    // Opens the font-size settings screen.
    private async void OnFontClicked(object? sender, EventArgs e)
        => await PushAsync<FontSettingsPage>();

    // Opens the speech-speed / pause settings screen.
    private async void OnSpeedClicked(object? sender, EventArgs e)
        => await PushAsync<SpeedSettingsPage>();

    // Resolves a page from DI and pushes it onto the navigation stack.
    private static Task PushAsync<TPage>() where TPage : Page
    {
        var page = IPlatformApplication.Current!.Services.GetRequiredService<TPage>();
        return Shell.Current.Navigation.PushAsync(page);
    }
}
