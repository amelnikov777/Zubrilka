using Zubrilka.ViewModels;

namespace Zubrilka.Views;

public partial class SpeedSettingsPage : ContentPage
{
    private readonly SpeedSettingsViewModel _viewModel;

    public SpeedSettingsPage(SpeedSettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Load stored speed/pause each time the screen opens.
        _viewModel.LoadCommand.Execute(null);
    }
}
