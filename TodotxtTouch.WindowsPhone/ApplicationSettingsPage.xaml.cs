using System;
using System.Windows;
using System.Windows.Navigation;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Phone.Controls;
using TodotxtTouch.WindowsPhone.Messages;
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

	    private void DisconnectFromDropBox_OnClick(object sender, RoutedEventArgs e)
	    {
            MessageBoxResult result = MessageBox.Show("This will de-authenticate this device from the current Dropbox account.\nAre you sure?\n(You can reconnect to Dropbox from the Settings screen or by tapping the sync button.)",
                "Disconnect", MessageBoxButton.OKCancel);

            if(result == MessageBoxResult.OK)
            {
                ((ApplicationSettingsViewModel) DataContext).DisconnectCommand.Execute(null);
            }
	    }

	    private void ConnectToDropBox_OnClick(object sender, RoutedEventArgs e)
	    {
            Messenger.Default.Register<CredentialsUpdatedMessage>(this, msg => ((ApplicationSettingsViewModel)DataContext).CheckConnection());
	        NavigationService.Navigate(new Uri("/DropboxLogin.xaml", UriKind.Relative));
	    }

        private void CustomizeColors_OnClick(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/CustomizeColorsPage.xaml", UriKind.Relative));
        }
	}
}