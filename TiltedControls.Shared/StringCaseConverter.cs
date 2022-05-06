using System;
#if NETFX_CORE
using Windows.UI.Xaml.Data;
#else
using Microsoft.UI.Xaml.Data;
#endif

namespace TiltedControls
{
    public class StringCaseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (parameter is string s)
            {
                if (value is bool b && b)
                {
                    return s.ToUpper();
                }
                return s;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return !(bool)value;
        }
    }
}
