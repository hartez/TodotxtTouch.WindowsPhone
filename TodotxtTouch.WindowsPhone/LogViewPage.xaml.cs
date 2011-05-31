using System.Windows;
using AgiliTrain.PhoneyTools;
using Microsoft.Phone.Controls;

namespace TodotxtTouch.WindowsPhone
{
	public partial class LogViewPage : PhoneApplicationPage
	{
		public LogViewPage()
		{
			InitializeComponent();
			Loaded += LogViewPage_Loaded;
		}

		private void LogViewPage_Loaded(object sender, RoutedEventArgs e)
		{
			Log.Text = PhoneLogger.LogContents;
		}
	}
}