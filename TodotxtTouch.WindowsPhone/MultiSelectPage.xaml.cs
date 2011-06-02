using System;
using System.Windows;
using System.Windows.Navigation;
using TodotxtTouch.WindowsPhone.ViewModel;

namespace TodotxtTouch.WindowsPhone
{
	public partial class MultiSelectPage : TaskFilterPage
	{
		public MultiSelectPage()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			BroadCastFilter();
		}

		private void MarkComplete_Click(object sender, EventArgs e)
		{
			((MainViewModel) DataContext).MarkSelectedTasksCompleteCommand.Execute(null);
			NavigationService.GoBack();
		}

		private void Delete_Click(object sender, EventArgs e)
		{
			var result = MessageBox.Show("Are you sure?", "Delete Tasks", MessageBoxButton.OKCancel);

			if (result == MessageBoxResult.OK)
			{
				((MainViewModel) DataContext).RemoveSelectedTasksCommand.Execute(null);
				NavigationService.GoBack();
			}
		}
	}
}