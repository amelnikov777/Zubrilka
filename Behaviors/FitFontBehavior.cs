using System.ComponentModel;

namespace Zubrilka.Behaviors;

/// <summary>
/// Shrinks a <see cref="Label"/>'s font size so its text fits the space it was given,
/// starting from <see cref="MaxFontSize"/> (the user's chosen size) down to
/// <see cref="MinFontSize"/>. Re-runs whenever the text or the label's size changes.
/// </summary>
public class FitFontBehavior : Behavior<Label>
{
    public static readonly BindableProperty MaxFontSizeProperty = BindableProperty.Create(
        nameof(MaxFontSize), typeof(double), typeof(FitFontBehavior), 28.0,
        propertyChanged: (b, _, _) => ((FitFontBehavior)b).Adjust());

    public static readonly BindableProperty MinFontSizeProperty = BindableProperty.Create(
        nameof(MinFontSize), typeof(double), typeof(FitFontBehavior), 12.0);

    // The largest size to use (normally the phrase text is shown at exactly this size).
    public double MaxFontSize
    {
        get => (double)GetValue(MaxFontSizeProperty);
        set => SetValue(MaxFontSizeProperty, value);
    }

    // The smallest size we will shrink to before giving up (text may then clip/scroll).
    public double MinFontSize
    {
        get => (double)GetValue(MinFontSizeProperty);
        set => SetValue(MinFontSizeProperty, value);
    }

    private Label? _label;
    private bool _adjusting; // guards against re-entrancy from our own FontSize changes

    protected override void OnAttachedTo(Label bindable)
    {
        base.OnAttachedTo(bindable);
        _label = bindable;
        bindable.SizeChanged += OnSizeChanged;
        bindable.PropertyChanged += OnLabelPropertyChanged;
    }

    protected override void OnDetachingFrom(Label bindable)
    {
        bindable.SizeChanged -= OnSizeChanged;
        bindable.PropertyChanged -= OnLabelPropertyChanged;
        _label = null;
        base.OnDetachingFrom(bindable);
    }

    private void OnSizeChanged(object? sender, EventArgs e) => Adjust();

    private void OnLabelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Re-fit when the shown text changes.
        if (e.PropertyName == Label.TextProperty.PropertyName)
            Adjust();
    }

    private void Adjust()
    {
        if (_label is null || _adjusting)
            return;

        // Need a known layout box to measure against.
        if (_label.Width <= 0 || _label.Height <= 0)
            return;

        // Empty text: reset to the maximum so the next phrase starts big.
        if (string.IsNullOrEmpty(_label.Text))
        {
            _label.FontSize = MaxFontSize;
            return;
        }

        _adjusting = true;
        try
        {
            double size = MaxFontSize;
            _label.FontSize = size;

            double availableWidth = _label.Width;
            double availableHeight = _label.Height;

            // Step down until the measured text fits the label's height, or we hit the minimum.
            while (size > MinFontSize)
            {
                var needed = ((IView)_label).Measure(availableWidth, double.PositiveInfinity);
                if (needed.Height <= availableHeight)
                    break;
                size -= 1;
                _label.FontSize = size;
            }
        }
        finally
        {
            _adjusting = false;
        }
    }
}
