using System;
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
			if (NavigationContext.QueryString.TryGetValue("filter", out filter))
			{
				Messenger.Default.Send<DrillDownMessage, MainViewModel>(new DrillDownMessage(filter));
			}
			else
			{
				Messenger.Default.Send<DrillDownMessage, MainViewModel>(new DrillDownMessage(String.Empty));
			}
		}
	}
}