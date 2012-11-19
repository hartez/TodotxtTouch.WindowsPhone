using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using Microsoft.Phone.Controls;
using TodotxtTouch.WindowsPhone.Messages;

namespace TodotxtTouch.WindowsPhone
{
	public partial class DropboxLogin : PhoneApplicationPage
	{
		public DropboxLogin()
		{
			InitializeComponent();

            Messenger.Default.Register<CredentialsUpdatedMessage>(this, msg =>
                                                                            {
                                                                                if(NavigationService.CanGoBack)
                                                                                {
                                                                                    NavigationService.GoBack();
                                                                                }
                                                                            });

			Messenger.Default.Register<RetrievedDropboxTokenMessage>(this, LoadLoginPage);

			loginBrowser.LoadCompleted += LoadCompleted;
		}

		private void LoadLoginPage(RetrievedDropboxTokenMessage msg)
		{
			if(!string.IsNullOrEmpty(msg.Error))
			{
				DispatcherHelper.CheckBeginInvokeOnUI(() => MessageBox.Show(msg.Error));
			}
			else
			{
				loginBrowser.Navigate(msg.TokenUri);	
			}
		}

		private void LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
		{
			//Check for the callback path here (or just check it against "/1/oauth/authorize")
			if (e.Uri.Host == "todotxt.traceur-llc.com")
			{
				Messenger.Default.Send(new DropboxLoginSuccessfulMessage());
			}
		}
	}
}