using GalaSoft.MvvmLight.Messaging;
using Microsoft.Phone.Controls;
using TodotxtTouch.WindowsPhone.Messages;

namespace TodotxtTouch.WindowsPhone
{
	public partial class DropboxLogin : PhoneApplicationPage
	{
		public DropboxLogin()
		{
			InitializeComponent();

			Messenger.Default.Register<CredentialsUpdatedMessage>(this, msg => NavigationService.GoBack());
			Messenger.Default.Register<CancelCredentialsUpdatedMessage>(this, msg => NavigationService.GoBack());
		}
	}
}