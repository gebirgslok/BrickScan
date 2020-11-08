using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace BrickScan.WpfClient.Converter
{
    internal class BooleanAndConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var booleanValues = values.Cast<bool>();
            var allTrue= booleanValues.All(x => x);
            return allTrue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException($"{nameof(ConvertBack)} on type {nameof(BooleanAndConverter)} is not supported.");
        }
    }
}
