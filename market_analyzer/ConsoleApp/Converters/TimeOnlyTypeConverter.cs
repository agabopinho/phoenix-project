using System.ComponentModel;
using System.Globalization;

namespace ConsoleApp.Converters;

public class TimeOnlyTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string stringValue)
            return TimeOnly.Parse(stringValue, culture);

        return base.ConvertFrom(context, culture, value);
    }
}