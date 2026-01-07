using System.Globalization;

namespace TB.DanceDance.Mobile.Converters;

public class ColorConverter : IValueConverter
{
    object IValueConverter.Convert(object? value, Type targetType, object? parameter, CultureInfo info)
    {
        if ((bool)value!)
            return Color.FromArgb(parameter!.ToString()); 
        else
            return Colors.Transparent;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}