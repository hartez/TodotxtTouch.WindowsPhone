using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using todotxtlib.net;
using TodotxtTouch.WindowsPhone.ViewModel;

namespace TodotxtTouch.WindowsPhone.ValueConverters
{
    public class TaskValueConverter : IValueConverter
    {
        private readonly ApplicationSettingsViewModel _settings;

        public TaskValueConverter()
        {
            _settings = ( (ViewModelLocator)Application.Current.Resources["Locator"] ).ApplicationSettings;
        }

        #region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var task = value as Task;

			if (task != null)
			{
				if (targetType == typeof (string))
				{
					string priority = task.IsPriority ? $"({task.Priority}) " : string.Empty;

					return priority + (string.IsNullOrEmpty(task.Body) ? "{Empty Task}" : task.Body);
				}

				if (targetType == typeof (Brush))
				{
					if (task.Completed)
					{
						return new SolidColorBrush(Colors.LightGray);
					}

					if (task.IsPriority)
					{
						var co = _settings?.PriorityColors.FirstOrDefault(pc => pc.Priority == task.Priority.ToUpper());
						if (co?.ColorOption.Color != null)
						{
							return new SolidColorBrush(co.ColorOption.Color.Value);
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