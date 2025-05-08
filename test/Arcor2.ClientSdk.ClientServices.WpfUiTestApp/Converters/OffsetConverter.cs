using System.Globalization;
using System.Windows.Data;

namespace Arcor2.ClientSdk.ClientServices.WpfUiTestApp.Converters;
public class OffsetConverter : IMultiValueConverter {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
        try {
            double a = System.Convert.ToDouble(values[0], culture);
            double b = System.Convert.ToDouble(values[1], culture);
            return a + b;
        }
        catch {
            return 0.0;
        }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
}