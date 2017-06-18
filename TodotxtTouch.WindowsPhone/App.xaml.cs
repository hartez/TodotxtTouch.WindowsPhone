﻿using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using TodotxtTouch.WindowsPhone.Messages;
using TodotxtTouch.WindowsPhone.ViewModel;

namespace TodotxtTouch.WindowsPhone
{
	public partial class App : Application
	{
		private const string StateKey = "State";

		/// <summary>
		/// Constructor for the Application object.
		/// </summary>
		public App()
		{
			Startup += App_Startup;

			// Global handler for uncaught exceptions. 
			UnhandledException += Application_UnhandledException;

			// Show graphics profiling information while debugging.
			if (Debugger.IsAttached)
			{
				// Display the current frame rate counters.
				//Current.Host.Settings.EnableFrameRateCounter = true;

				// Show the areas of the app that are being redrawn in each frame.
				//Application.Current.Host.Settings.EnableRedrawRegions = true;

				// Enable non-production analysis visualization mode, 
				// which shows areas of a page that are being GPU accelerated with a colored overlay.
				//Application.Current.Host.Settings.EnableCacheVisualization = true;
			}

			// Standard Silverlight initialization
			InitializeComponent();

			// Phone-specific initialization
			InitializePhoneApplication();

			// Uncomment the next line to test the app without network connectivity
			//NetworkHelper.TestNeverAvailable();

			Messenger.Default.Register<NetworkUnavailableMessage>(this,
				msg =>
					MessageBox.Show("The network is currently unavailable", "Error",
						MessageBoxButton.OK));

			Messenger.Default.Register<SynchronizationErrorMessage>(this, msg =>
				MessageBox.Show(
					"An error occurred while syncing; you may need to try again later\n" +
					msg.Exception.Message,
					"Error", MessageBoxButton.OK));

			Messenger.Default.Register<ArchiveErrorMessage>(this, msg =>
				MessageBox.Show(
					"An error occurred while archiving\n" +
					msg.Exception.Message,
					"Error", MessageBoxButton.OK));

			Messenger.Default.Register<AuthenticationErrorMessage>(this, msg => MessageBox.Show(
				"An error occurred while authenticating to Dropbox\n" +
				msg.Exception.Message,
				"Error", MessageBoxButton.OK));

			Messenger.Default.Register<CannotAccessDropboxMessage>(this, msg => MessageBox.Show(
				"Cannot access Dropbox\n" +
				msg.Reason,
				"Error", MessageBoxButton.OK));
		}

		/// <summary>
		/// Provides easy access to the root frame of the Phone Application.
		/// </summary>
		/// <returns>The root frame of the Phone Application.</returns>
		public PhoneApplicationFrame RootFrame { get; private set; }

		private void App_Startup(object sender, StartupEventArgs e)
		{
			DispatcherHelper.Initialize();
		}

		// Code to execute when the application is launching (eg, from Start)
		// This code will not execute when the application is reactivated
		private void Application_Launching(object sender, LaunchingEventArgs e)
		{
			LittleWatson.CheckForPreviousException("Todo.txt Windows Phone 7 error report",
				"support@codewise-llc.com");
			Messenger.Default.Send(new ApplicationReadyMessage());
			Messenger.Default.Send(new ApplicationStartedMessage());
		}

		// Code to execute when the application is activated (brought to foreground)
		// This code will not execute when the application is first launched
		private void Application_Activated(object sender, ActivatedEventArgs e)
		{
			var viewModel = ((ViewModelLocator) Current.Resources["Locator"]).Main;

			if (viewModel != null)
			{
				var state = TombstoneState.FromJson(PhoneApplicationService.Current.State[StateKey].ToString());

				viewModel.SetState(state);
			}

			Messenger.Default.Send(new ApplicationReadyMessage());
		}

		public static void UpdateBindingOnFocusedTextBox()
		{
			// Focus kludge to make the binding in the textbox update
			var focusObj = FocusManager.GetFocusedElement();
			if (focusObj is TextBox)
			{
				var binding = (focusObj as TextBox).GetBindingExpression(TextBox.TextProperty);
				binding?.UpdateSource();
			}
		}

		// Code to execute when the application is deactivated (sent to background)
		// This code will not execute when the application is closing
		private void Application_Deactivated(object sender, DeactivatedEventArgs e)
		{
			UpdateBindingOnFocusedTextBox();

			var viewModel = ((ViewModelLocator) Current.Resources["Locator"]).Main;

			if (viewModel != null)
			{
				var selectedTask = string.Empty;
				if (viewModel.SelectedTask != null)
				{
					selectedTask = viewModel.SelectedTask.ToString();
				}

				var draft = string.Empty;
				if (viewModel.SelectedTaskDraft != null)
				{
					draft = viewModel.SelectedTaskDraft.ToString();
				}

				var state = new TombstoneState(selectedTask, draft);
				if (PhoneApplicationService.Current.State.ContainsKey(StateKey))
				{
					PhoneApplicationService.Current.State[StateKey] = TombstoneState.ToJson(state);
				}
				else
				{
					PhoneApplicationService.Current.State.Add(StateKey, TombstoneState.ToJson(state));
				}
			}
		}

		// Code to execute when the application is closing (eg, user hit Back)
		// This code will not execute when the application is deactivated
		private void Application_Closing(object sender, ClosingEventArgs e)
		{
		}

		// Code to execute if a navigation fails
		private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
		{
			if (Debugger.IsAttached)
			{
				// A navigation has failed; break into the debugger
				Debugger.Break();
			}
		}

		// Code to execute on Unhandled Exceptions
		private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
		{
			LittleWatson.ReportException(e.ExceptionObject,
				$"Version {Assembly.GetExecutingAssembly().FullName.Split('=')[1].Split(',')[0]}");

			if (Debugger.IsAttached)
			{
				// An unhandled exception has occurred; break into the debugger
				Debugger.Break();
			}
		}

		#region Phone application initialization

		// Avoid double-initialization
		private bool _phoneApplicationInitialized;

		// Do not add any additional code to this method
		private void InitializePhoneApplication()
		{
			if (_phoneApplicationInitialized)
			{
				return;
			}

			// Create the frame but don't set it as RootVisual yet; this allows the splash
			// screen to remain active until the application is ready to render.
			RootFrame = new PhoneApplicationFrame();
			RootFrame.Navigated += CompleteInitializePhoneApplication;

			// Handle navigation failures
			RootFrame.NavigationFailed += RootFrame_NavigationFailed;

			// Ensure we don't initialize again
			_phoneApplicationInitialized = true;
		}

		// Do not add any additional code to this method
		private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
		{
			// Set the root visual to allow the application to render
			if (RootVisual != RootFrame)
			{
				RootVisual = RootFrame;
			}

			// Remove this handler since it is no longer needed
			RootFrame.Navigated -= CompleteInitializePhoneApplication;
		}

		#endregion
	}
}