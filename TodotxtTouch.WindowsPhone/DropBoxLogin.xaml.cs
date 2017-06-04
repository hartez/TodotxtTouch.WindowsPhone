using System;
using System.Windows;
using Dropbox.Api;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using Microsoft.Phone.Controls;
using TodotxtTouch.WindowsPhone.Messages;
using TodotxtTouch.WindowsPhone.ViewModel;

namespace TodotxtTouch.WindowsPhone
{
	public partial class DropboxLogin : PhoneApplicationPage
	{
		public DropboxLogin()
		{
			InitializeComponent();

			Messenger.Default.Register<CredentialsUpdatedMessage>(this,
				msg =>
				{
					if (NavigationService.CanGoBack)
					{
						NavigationService.GoBack();
					}
				});

			Messenger.Default.Register<RetrievedDropboxTokenMessage>(this, LoadLoginPage);

			LoginBrowser.Navigating += Navigating;
		}

		protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ((DropboxCredentialsViewModel)DataContext).StartLoginProcessCommand.Execute(null);
        }

		private void LoadLoginPage(RetrievedDropboxTokenMessage msg)
		{
			if(!string.IsNullOrEmpty(msg.Error))
			{
				DispatcherHelper.CheckBeginInvokeOnUI(() => MessageBox.Show(msg.Error));
			}
			else
			{
				LoginBrowser.Navigate(msg.TokenUri);	
			}
		}

		private void Navigating(object sender, NavigatingEventArgs e)
		{
			if (e.Uri.Host != "todotxt.codewise-llc.com")
			{
				return;
			}

			Messenger.Default.Send(new DropboxLoginSuccessfulMessage(e.Uri));
		}
	}
}