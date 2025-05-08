using System.Globalization;
using System.Windows.Data;

namespace Arcor2.ClientSdk.ClientServices.WpfUiTestApp.Converters;

[ValueConversion(typeof(Enum), typeof(bool))]
public class EnumToBoolConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        if(value != null && parameter is string enumValue && Enum.IsDefined(value.GetType(), value)) {
            return value.ToString() == enumValue;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        if(value is bool boolValue && boolValue && parameter is string enumValue) {
            return Enum.Parse(targetType, enumValue);
        }
        return null;
    }
}