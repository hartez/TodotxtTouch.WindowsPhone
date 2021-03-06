﻿using System;
using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using TodotxtTouch.WindowsPhone.ViewModel;

namespace TodotxtTouch.WindowsPhone
{
	public partial class MainPage : PhoneApplicationPage
	{
		public MainPage()
		{
			InitializeComponent();

			Messenger.Default.Register<NeedCredentialsMessage>(
				this, (message) => ShowLogin());

			Messenger.Default.Register<TaskLoadingState>(
				this, LoadingStateChanged);

			Messenger.Default.Register<ViewTaskMessage>(
				this, ViewSelectedTask);

			Messenger.Default.Register<CredentialsUpdatedMessage>(
				this, (message) => HideLogin());

			((ApplicationBarIconButton) ApplicationBar.Buttons[0]).Click += AddButton_Click;

			Loaded += MainPage_Loaded;
		}

		void MainPage_Loaded(object sender, RoutedEventArgs e)
		{
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
			TaskList.Visibility = Visibility.Collapsed;
			ApplicationBar.IsVisible = false;
			//SyncButton.Visibility = Visibility.Collapsed;
		}

		private void HideLogin()
		{
			DropBoxLogin.Visibility = Visibility.Collapsed;
			TaskList.Visibility = Visibility.Visible;
			ApplicationBar.IsVisible = true;
			//SyncButton.Visibility = Visibility.Visible;
		}


		private void LoadingStateChanged(TaskLoadingState taskLoadingState)
		{
			switch (taskLoadingState)
			{
				case TaskLoadingState.NotLoaded:
					TaskList.Visibility = Visibility.Collapsed;
					//SyncButton.Visibility = Visibility.Collapsed;
					break;
				case TaskLoadingState.Syncing:
					TaskList.Visibility = Visibility.Collapsed;
					// Todo display loading message or something
					break;
				case TaskLoadingState.Ready:
					DropBoxLogin.Visibility = Visibility.Collapsed;
					TaskList.Visibility = Visibility.Visible;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}