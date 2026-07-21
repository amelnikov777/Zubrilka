using System.Windows.Input;

namespace Zubrilka.Behaviors;

/// <summary>
/// Adds "tap" and "long-press" handling to any View using a single pointer gesture,
/// so the two never fire together (a long-press does NOT also raise a tap on release).
/// Replaces CommunityToolkit.Maui's TouchBehavior, which we don't take as a dependency.
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
public class TapAndHoldBehavior : Behavior<View>
{
    // How long the press must be held to count as a long-press.
    private static readonly TimeSpan LongPressThreshold = TimeSpan.FromMilliseconds(500);

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

    /// <summary>Invoked once the press has been held past the threshold.</summary>
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

    private IDispatcherTimer? _holdTimer;   // fires once if the press is held long enough
    private bool _longPressFired;           // guards the release so tap doesn't also run
    private View? _view;

    protected override void OnAttachedTo(View bindable)
    {
        base.OnAttachedTo(bindable);
        _view = bindable;

        // One pointer recognizer drives both gestures.
        var pointer = new PointerGestureRecognizer();
        pointer.PointerPressed += OnPressed;
        pointer.PointerReleased += OnReleased;
        pointer.PointerExited += OnCancelled;   // finger left the element (e.g. during a scroll)
        bindable.GestureRecognizers.Add(pointer);
    }

    protected override void OnDetachingFrom(View bindable)
    {
        // Remove our recognizer and stop any pending timer to avoid leaks.
        StopTimer();
        var pointer = bindable.GestureRecognizers.OfType<PointerGestureRecognizer>().FirstOrDefault();
        if (pointer is not null)
        {
            pointer.PointerPressed -= OnPressed;
            pointer.PointerReleased -= OnReleased;
            pointer.PointerExited -= OnCancelled;
            bindable.GestureRecognizers.Remove(pointer);
        }
        _view = null;
        base.OnDetachingFrom(bindable);
    }

    private void OnPressed(object? sender, PointerEventArgs e)
    {
        _longPressFired = false;

        // Start (or restart) the hold timer on the UI dispatcher.
        StopTimer();
        _holdTimer = _view?.Dispatcher.CreateTimer();
        if (_holdTimer is null)
            return;

        _holdTimer.Interval = LongPressThreshold;
        _holdTimer.IsRepeating = false;
        _holdTimer.Tick += (_, _) =>
        {
            StopTimer();
            _longPressFired = true;
            Invoke(LongPressCommand); // held long enough -> long-press
        };
        _holdTimer.Start();
    }

    private void OnReleased(object? sender, PointerEventArgs e)
    {
        StopTimer();
        // If the long-press already fired, swallow the release; otherwise it's a tap.
        if (!_longPressFired)
            Invoke(TapCommand);
    }

    private void OnCancelled(object? sender, PointerEventArgs e)
    {
        // Finger moved off before the threshold: neither gesture should fire.
        StopTimer();
    }

    private void Invoke(ICommand? command)
    {
        if (command is not null && command.CanExecute(CommandParameter))
            command.Execute(CommandParameter);
    }

    private void StopTimer()
    {
        if (_holdTimer is not null)
        {
            _holdTimer.Stop();
            _holdTimer = null;
        }
    }
}
