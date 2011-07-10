using System;
using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Phone.Shell;
using TodotxtTouch.WindowsPhone.ViewModel;

namespace TodotxtTouch.WindowsPhone
{
	public partial class MainPivot : TaskFilterPage
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

			Messenger.Default.Register<DrillDownMessage>(this, DrillDown);
				
			((ApplicationBarIconButton)ApplicationBar.Buttons[0]).Click += AddButton_Click;
			((ApplicationBarIconButton)ApplicationBar.Buttons[1]).Click += MultiSelect_Click;
			((ApplicationBarIconButton)ApplicationBar.Buttons[2]).Click += SyncButton_Click;

			Loaded += MainPage_Loaded;
		}

		private void SyncButton_Click(object sender, EventArgs e)
		{
			((MainViewModel)DataContext).SyncCommand.Execute(null);
		}

		private void MultiSelect_Click(object sender, EventArgs e)
		{
			string filter;
			if (NavigationContext.QueryString.TryGetValue("filter", out filter))
			{
				NavigationService.Navigate(
			   new Uri("/MultiSelectPage.xaml?filter=" + filter, UriKind.Relative));
			}
			else
			{
				NavigationService.Navigate(
			   new Uri("/MultiSelectPage.xaml", UriKind.Relative));
			}
		}

		private void DrillDown(DrillDownMessage message)
		{
			 NavigationService.Navigate(
			   new Uri("/MainPivot.xaml?filter=" + message.Filter, UriKind.Relative));
		}

		protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			BroadCastFilter();
		}

		private void MainPage_Loaded(object sender, RoutedEventArgs e)
		{
			LittleWatson.CheckForPreviousException();	

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
				case TaskLoadingState.Syncing:
					TaskPivot.Visibility = Visibility.Collapsed;
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

		private void SettingsClick(object sender, EventArgs e)
		{
			NavigationService.Navigate(new Uri("/ApplicationSettingsPage.xaml", UriKind.Relative));
		}
	}
}

