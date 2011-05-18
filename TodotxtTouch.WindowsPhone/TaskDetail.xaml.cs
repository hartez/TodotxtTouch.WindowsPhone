using System;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using TodotxtTouch.WindowsPhone.ViewModel;

namespace TodotxtTouch.WindowsPhone
{
	public partial class TaskDetail : PhoneApplicationPage
	{
		public TaskDetail()
		{
			InitializeComponent();

			((ApplicationBarIconButton)ApplicationBar.Buttons[0]).Click += SaveButton_Click;
			((ApplicationBarIconButton)ApplicationBar.Buttons[1]).Click += CancelButton_Click;
		}

		private void CancelButton_Click(object sender, EventArgs e)
		{
			NavigationService.GoBack();
		}

		private void SaveButton_Click(object sender, EventArgs e)
		{
			((MainViewModel)DataContext).SaveCurrentTaskCommand.Execute(null);
			NavigationService.GoBack();
		}
	}
}