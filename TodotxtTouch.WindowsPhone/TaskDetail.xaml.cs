using System;
using System.Windows.Controls;
using System.Windows.Input;
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
			// TODO this should issue a revert command
		}

		private void SaveButton_Click(object sender, EventArgs e)
		{
			// Focus kludge to make the binding in the textbox update
			object focusObj = FocusManager.GetFocusedElement();
			if (focusObj != null && focusObj is TextBox)
			{
				var binding = (focusObj as TextBox).GetBindingExpression(TextBox.TextProperty);
				binding.UpdateSource();
			}

			((MainViewModel)DataContext).SaveCurrentTaskCommand.Execute(null);
			NavigationService.GoBack();
		}
	}
}