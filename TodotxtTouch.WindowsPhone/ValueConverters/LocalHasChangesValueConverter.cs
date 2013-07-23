using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace TodotxtTouch.WindowsPhone.ValueConverters
{
    public class LocalHasChangesValueConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var hasChanges = (bool) value;

            if(targetType == typeof(Brush))
            {
                if(hasChanges)
                {
                    if(Visibility.Visible == (Visibility) Application.Current.Resources["PhoneDarkThemeVisibility"])
                    {
                        return new SolidColorBrush(Colors.Yellow);
                    }

                    return new SolidColorBrush(Colors.Orange);
                }

                return Application.Current.Resources["PhoneForegroundBrush"];
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        #endregion
    }
}