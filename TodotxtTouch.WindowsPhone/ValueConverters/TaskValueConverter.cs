using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using todotxtlib.net;

namespace TodotxtTouch.WindowsPhone.ValueConverters
{
    public class VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var visible = (bool)value;

            if (visible)
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

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

					return priority + (String.IsNullOrEmpty(task.Body) ? "{Empty Task}" : task.Body);
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

                        // TODO Check for color customization; if so, use those values
                        // otherwise, use these defaults (including the Light Theme workaround)

						switch ((int) priority)
						{
							case (65):

								if(Visibility.Visible==(Visibility)Application.Current.Resources["PhoneDarkThemeVisibility"])
								{
									return new SolidColorBrush(Colors.Yellow);
								}
								
								// Yellow is the standard for the other todo.txt projects,
								// but it's impossible to see if the user is using the Light theme
								return new SolidColorBrush(Colors.Orange);
								
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