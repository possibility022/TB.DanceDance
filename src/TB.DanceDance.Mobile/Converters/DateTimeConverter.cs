using System.Globalization;

namespace TB.DanceDance.Mobile.Converters;

public class DateTimeConverter : IValueConverter
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("pl-PL");
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        DateTime dateTime = (DateTime)value;
        return dateTime.ToString("D", Culture);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}