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
					    if (_settings != null)
					    {
                            var co = _settings.PriorityColors.FirstOrDefault(pc => pc.Priority == task.Priority.ToUpper());
					        if (co != null)
					        {
					            if (co.ColorOption.Color.HasValue)
					            {
                                    return new SolidColorBrush(co.ColorOption.Color.Value);
					            }
					        }
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