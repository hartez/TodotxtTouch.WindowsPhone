using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using todotxtlib.net;

namespace TodotxtTouch.WindowsPhone.ValueConverters
{
	public class TaskValueConverter : IValueConverter
	{
		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var task = value as Task;

			if (task != null)
			{
				if (targetType == typeof (String))
				{
					string priority = task.IsPriority ? string.Format("({0}) ", task.Priority) : String.Empty;

					return priority + task.Body;
				}

				if (targetType == typeof (Brush))
				{
					if (task.Completed)
					{
						return new SolidColorBrush(Colors.LightGray);
					}

					if (task.IsPriority)
					{
						char priority = task.Priority.ToUpper()[0];

						switch ((int) priority)
						{
							case (65):
								return new SolidColorBrush(Colors.Yellow);
							case (66):
								return new SolidColorBrush(Colors.Green);
							case (67):
								return new SolidColorBrush(Colors.Cyan);
						}
					}

					return Application.Current.Resources["PhoneForegroundBrush"];
				}
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