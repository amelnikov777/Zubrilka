using System.Globalization;

namespace Zubrilka.Converters;

/// <summary>
/// Converts a "right-to-left" boolean into a <see cref="FlowDirection"/> so RTL languages
/// (Hebrew, Arabic, …) display their phrases correctly.
/// </summary>
public class BoolToFlowDirectionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is FlowDirection.RightToLeft;
}
