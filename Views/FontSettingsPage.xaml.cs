using Zubrilka.ViewModels;

namespace Zubrilka.Views;

public partial class FontSettingsPage : ContentPage
{
    private readonly FontSettingsViewModel _viewModel;

    public FontSettingsPage(FontSettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Load the stored size each time the screen opens.
        _viewModel.LoadCommand.Execute(null);
    }
}
