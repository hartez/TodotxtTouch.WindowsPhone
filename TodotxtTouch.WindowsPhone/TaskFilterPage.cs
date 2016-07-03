using GalaSoft.MvvmLight.Messaging;
using Microsoft.Phone.Controls;
using TodotxtTouch.WindowsPhone.Messages;
using TodotxtTouch.WindowsPhone.ViewModel;

namespace TodotxtTouch.WindowsPhone
{
	public class TaskFilterPage : PhoneApplicationPage
	{
		public void BroadCastFilter()
		{
			string filter;
			Messenger.Default.Send<DrillDownMessage, MainViewModel>(
				NavigationContext.QueryString.TryGetValue("filter", out filter)
				? new DrillDownMessage(filter)
				: new DrillDownMessage(string.Empty));
		}
	}
}