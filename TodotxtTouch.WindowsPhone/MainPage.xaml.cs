using System;
using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Phone.Controls;
using TodotxtTouch.WindowsPhone.ViewModel;

namespace TodotxtTouch.WindowsPhone
{
	public partial class MainPage : PhoneApplicationPage
	{
		public MainPage()
		{
			InitializeComponent();

			Messenger.Default.Register<NeedCredentialsMessage>(
				this, (message) => ShowDropboxCredentialsPage());

			Messenger.Default.Register<LoadingStateChangedMessage>(
				this, LoadingStateChanged);
		}

		private void LoadingStateChanged(LoadingStateChangedMessage message)
		{
			switch (message.State)
			{
				case TaskLoadingState.NotLoaded:
					TaskList.Visibility = Visibility.Collapsed;
					break;
				case TaskLoadingState.Loading:
					TaskList.Visibility = Visibility.Collapsed;
					// Todo display loading message or something
					break;
				case TaskLoadingState.Loaded:
					DropBoxLogin.Visibility = Visibility.Collapsed;
					TaskList.Visibility = Visibility.Visible;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void ShowDropboxCredentialsPage()
		{
			DropBoxLogin.Visibility = Visibility.Visible;
		}
	}
}