using System;
using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Phone.Shell;
using TodotxtTouch.WindowsPhone.Messages;
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

			Messenger.Default.Register<ViewTaskMessage>(
				this, ViewSelectedTask);

			Messenger.Default.Register<InitiateCallMessage>(this, message => MessageBox.Show("call"));

			Messenger.Default.Register<DrillDownMessage>(this, DrillDown);

			((ApplicationBarIconButton)ApplicationBar.Buttons[0]).Click += AddButton_Click;
			((ApplicationBarIconButton)ApplicationBar.Buttons[1]).Click += MultiSelectClick;
			((ApplicationBarIconButton)ApplicationBar.Buttons[2]).Click += SyncButtonClick;
		}

		private void SyncButtonClick(object sender, EventArgs e)
		{
			StartSync();
		}

		private void StartSync()
		{
			Messenger.Default.Unregister<CredentialsUpdatedMessage>(this);
			((MainViewModel)DataContext).SyncCommand.Execute(null);
		}

		private void MultiSelectClick(object sender, EventArgs e)
		{
			string filter;
			if (NavigationContext.QueryString.TryGetValue("filter", out filter))
			{
				NavigationService.Navigate(new Uri("/MultiSelectPage.xaml?filter=" + filter, UriKind.Relative));
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
			Messenger.Default.Register<CredentialsUpdatedMessage>(this, msg => StartSync());
			NavigationService.Navigate(new Uri("/DropboxLogin.xaml", UriKind.Relative));
		}

		private void ArchiveClick(object sender, EventArgs e)
		{
			((MainViewModel)DataContext).ArchiveTasksCommand.Execute(null);
		}

		private void AboutClick(object sender, EventArgs e)
		{
			NavigationService.Navigate(new Uri("/About.xaml", UriKind.Relative));
		}

		private void SettingsClick(object sender, EventArgs e)
		{
			NavigationService.Navigate(new Uri("/ApplicationSettingsPage.xaml", UriKind.Relative));
		}
	}
}

