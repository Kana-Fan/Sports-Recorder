using System.Globalization;
using System.Windows.Data;

namespace SportsRecorder
{
    public class BodyPartToNamesConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is string bodyPart &&
                values[1] is Dictionary<string, List<string>> strengthTypes &&
                strengthTypes.TryGetValue(bodyPart, out var names))
            {
                return names;
            }

            return new List<string>();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
