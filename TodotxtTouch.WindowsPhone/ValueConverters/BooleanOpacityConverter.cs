using System;
using System.Globalization;
using System.Windows.Data;

namespace TodotxtTouch.WindowsPhone.ValueConverters
{
	public class BooleanOpacityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			double opacity = 1.0;
			if (parameter != null)
			{
				opacity = double.Parse(parameter.ToString(), CultureInfo.InvariantCulture);
			}

			if((bool)value)
			{
				return opacity;
			}

			return 1.0;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value;
		}
	}
}