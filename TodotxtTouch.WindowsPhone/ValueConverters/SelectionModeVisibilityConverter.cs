using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace TodotxtTouch.WindowsPhone.ValueConverters
{
	public class SelectionModeVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var selectionMode = (SelectionMode) value;

			switch(selectionMode)
			{
				case SelectionMode.Single:
					return Visibility.Collapsed;
				case SelectionMode.Multiple:
				case SelectionMode.Extended:
					return Visibility.Visible;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value;
		}
	}
}