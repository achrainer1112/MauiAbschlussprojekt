using System.Globalization;

namespace MauiAbschlussprojekt.Converters
{

    public class StringNotNullOrEmptyBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool hasValue = !string.IsNullOrWhiteSpace(value as string);
            bool invert = parameter?.ToString()?.ToLower() == "false";
            return invert ? !hasValue : hasValue;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }


    public class IntToBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool hasValue = value is int i && i > 0;
            bool invert = parameter?.ToString()?.ToLower() == "false";
            return invert ? !hasValue : hasValue;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}