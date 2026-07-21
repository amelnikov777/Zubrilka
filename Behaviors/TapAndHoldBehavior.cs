using System.Windows.Input;
using AView = Android.Views.View;

namespace Zubrilka.Behaviors;

/// <summary>
/// Adds "tap" and "long-press" handling to any View, so the two never fire together
/// (a long-press does NOT also raise a tap on release).
///
/// Built on Android's native Click/LongClick rather than a PointerGestureRecognizer:
/// on Android pointer events only report mouse/stylus input, so a finger tap never
/// raised PointerPressed/PointerReleased and neither gesture fired. Android also
/// suppresses the click that would follow a consumed long-click, which is exactly the
/// semantics we want — no hold timer to manage. The long-press threshold is the
/// platform's own (~500 ms).
///
/// Usage in XAML:
///   &lt;Border&gt;
///     &lt;Border.Behaviors&gt;
///       &lt;b:TapAndHoldBehavior TapCommand="{Binding OpenCmd}"
///                              LongPressCommand="{Binding DeleteCmd}"
///                              CommandParameter="{Binding .}" /&gt;
///     &lt;/Border.Behaviors&gt;
///   &lt;/Border&gt;
/// </summary>
public class TapAndHoldBehavior : PlatformBehavior<View, AView>
{
    public static readonly BindableProperty TapCommandProperty =
        BindableProperty.Create(nameof(TapCommand), typeof(ICommand), typeof(TapAndHoldBehavior));

    public static readonly BindableProperty LongPressCommandProperty =
        BindableProperty.Create(nameof(LongPressCommand), typeof(ICommand), typeof(TapAndHoldBehavior));

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(TapAndHoldBehavior));

    /// <summary>Invoked on a short press-and-release.</summary>
    public ICommand? TapCommand
    {
        get => (ICommand?)GetValue(TapCommandProperty);
        set => SetValue(TapCommandProperty, value);
    }

    /// <summary>Invoked once the press has been held past the platform threshold.</summary>
    public ICommand? LongPressCommand
    {
        get => (ICommand?)GetValue(LongPressCommandProperty);
        set => SetValue(LongPressCommandProperty, value);
    }

    /// <summary>Parameter passed to whichever command fires (usually the bound item).</summary>
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    protected override void OnAttachedTo(View bindable, AView platformView)
    {
        // Unlike Behavior<T>, PlatformBehavior does not inherit the view's BindingContext,
        // so the command bindings would resolve against nothing. Mirror it here and keep it
        // in sync — CollectionView recycles rows, so a row's context changes as the list scrolls.
        BindingContext = bindable.BindingContext;
        bindable.BindingContextChanged += OnBindingContextChanged;

        // A Border's platform view isn't clickable by default; opt in to both gestures.
        platformView.Clickable = true;
        platformView.LongClickable = true;
        platformView.Click += OnClick;
        platformView.LongClick += OnLongClick;
    }

    protected override void OnDetachedFrom(View bindable, AView platformView)
    {
        bindable.BindingContextChanged -= OnBindingContextChanged;
        platformView.Click -= OnClick;
        platformView.LongClick -= OnLongClick;
    }

    private void OnBindingContextChanged(object? sender, EventArgs e)
    {
        if (sender is View view)
            BindingContext = view.BindingContext;
    }

    private void OnClick(object? sender, EventArgs e) => Invoke(TapCommand);

    private void OnLongClick(object? sender, AView.LongClickEventArgs e)
    {
        // Marking it handled stops Android from raising the trailing Click.
        e.Handled = true;
        Invoke(LongPressCommand);
    }

    private void Invoke(ICommand? command)
    {
        if (command is not null && command.CanExecute(CommandParameter))
            command.Execute(CommandParameter);
    }
}
