using System;
using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using TodotxtTouch.WindowsPhone.ViewModel;

namespace TodotxtTouch.WindowsPhone
{
	public partial class MainPivot : PhoneApplicationPage
	{
		public MainPivot()
		{
			InitializeComponent();

			Messenger.Default.Register<NeedCredentialsMessage>(
				this, (message) => ShowLogin());

			Messenger.Default.Register<TaskLoadingState>(
				this, LoadingStateChanged);

			Messenger.Default.Register<ViewTaskMessage>(
				this, ViewSelectedTask);

			Messenger.Default.Register<CredentialsUpdatedMessage>(
				this, message => HideLogin());


			// TODO Change drill-down to work off of URI instead of Observable Stack
			// otherwise, reloading the app doesn't work correctly
			Messenger.Default.Register<DrillDownMessage>(this, DrillDown);

				
			((ApplicationBarIconButton) ApplicationBar.Buttons[0]).Click += AddButton_Click;

			Loaded += MainPage_Loaded;
		}

		private void DrillDown(DrillDownMessage message)
		{
			 NavigationService.Navigate(
			   new Uri("/MainPivot.xaml?filter=" + message.Filter, UriKind.Relative));
		}


		protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			string filter;
			if(NavigationContext.QueryString.TryGetValue("filter", out filter))
			{
				Messenger.Default.Send<DrillDownMessage, MainViewModel>(new DrillDownMessage(filter));
			}
			else
			{
				Messenger.Default.Send<DrillDownMessage, MainViewModel>(new DrillDownMessage(String.Empty));
			}
		}

		private void MainPage_Loaded(object sender, RoutedEventArgs e)
		{
			DropBoxLogin.Opacity = 1;
			Messenger.Default.Send(new ApplicationReadyMessage());
		}

		private void AddButton_Click(object sender, EventArgs e)
		{
			((MainViewModel) DataContext).AddTaskCommand.Execute(null);
		}

		private void ViewSelectedTask(ViewTaskMessage obj)
		{
			NavigationService.Navigate(new Uri("/TaskDetail.xaml", UriKind.Relative));
		}

		private void ShowLogin()
		{
			DropBoxLogin.Visibility = Visibility.Visible;
			TaskPivot.Visibility = Visibility.Collapsed;
			ApplicationBar.IsVisible = false;
		}

		private void HideLogin()
		{
			DropBoxLogin.Visibility = Visibility.Collapsed;
			TaskPivot.Visibility = Visibility.Visible;
			ApplicationBar.IsVisible = true;
		}


		private void LoadingStateChanged(TaskLoadingState taskLoadingState)
		{
			switch (taskLoadingState)
			{
				case TaskLoadingState.NotLoaded:
					TaskPivot.Visibility = Visibility.Collapsed;
					break;
				case TaskLoadingState.Syncing:
					TaskPivot.Visibility = Visibility.Collapsed;
					// Todo display loading message or something
					break;
				case TaskLoadingState.Ready:
					DropBoxLogin.Visibility = Visibility.Collapsed;
					TaskPivot.Visibility = Visibility.Visible;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void ArchiveClick(object sender, EventArgs e)
		{
			((MainViewModel)DataContext).ArchiveTasksCommand.Execute(null);
		}
	}
}

