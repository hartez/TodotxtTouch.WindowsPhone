using System;
using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Phone.Controls;
using TodotxtTouch.WindowsPhone.ViewModel;

namespace TodotxtTouch.WindowsPhone
{
	public partial class Login : PhoneApplicationPage
	{
		public Login()
		{
			InitializeComponent();

			Loaded += LoginLoaded;
		}

		private void LoginLoaded(object sender, RoutedEventArgs e)
		{
			Messenger.Default.Register<LoadingStateChangedMessage>(
				this, LoadingStateChanged);
		}

		private void LoadingStateChanged(LoadingStateChangedMessage message)
		{
			switch (message.State)
			{
				case TaskLoadingState.Loading:
					// Todo display loading message or something
					break;
				case TaskLoadingState.Loaded:
					// Go back to the main page
					NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}