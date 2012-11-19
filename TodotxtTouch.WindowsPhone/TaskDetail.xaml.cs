using System;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using TodotxtTouch.WindowsPhone.ViewModel;

namespace TodotxtTouch.WindowsPhone
{
	public partial class TaskDetail : PhoneApplicationPage
	{
	    private bool _navigationInProgress;

	    public TaskDetail()
		{
			InitializeComponent();

			((ApplicationBarIconButton) ApplicationBar.Buttons[0]).Click += SaveButton_Click;
			((ApplicationBarIconButton) ApplicationBar.Buttons[1]).Click += CancelButton_Click;
		}

		private void CancelButton_Click(object sender, EventArgs e)
		{
			((MainViewModel)DataContext).RevertCurrentTaskCommand.Execute(null);
		}

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (_navigationInProgress)
            {
                e.Cancel = true;
                return;
            }
            _navigationInProgress = true;
            base.OnNavigatingFrom(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            _navigationInProgress = false;
        }

		private void SaveButton_Click(object sender, EventArgs e)
		{
			App.UpdateBindingOnFocusedTextBox();

			((MainViewModel) DataContext).SaveCurrentTaskCommand.Execute(null);
            
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
		}
	}
}