using System.Windows.Navigation;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Phone.Controls;
using TodotxtTouch.WindowsPhone.ViewModel;

namespace TodotxtTouch.WindowsPhone
{
	public partial class ApplicationSettingsPage : PhoneApplicationPage
	{
		public ApplicationSettingsPage()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			((ApplicationSettingsViewModel) DataContext).BroadcastSettingsChanged.Execute(null);
			Messenger.Default.Send(new ApplicationReadyMessage());
		}
	}
}