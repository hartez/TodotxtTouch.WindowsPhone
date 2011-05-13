using System;
using System.IO.IsolatedStorage;
using DropNet;
using DropNet.Models;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using RestSharp;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
	/// <summary>
	/// This class contains properties that the main View can data bind to.
	/// <para>
	/// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
	/// </para>
	/// <para>
	/// You can also use Blend to data bind with the tool's support.
	/// </para>
	/// <para>
	/// See http://www.galasoft.ch/mvvm
	/// </para>
	/// </summary>
	public class MainViewModel : ViewModelBase
	{
		/// <summary>
		/// The <see cref="Username" /> property's name.
		/// </summary>
		public const string UsernamePropertyName = "Username";

		/// <summary>
		/// The <see cref="Password" /> property's name.
		/// </summary>
		public const string PasswordPropertyName = "Password";

		/// <summary>
		/// The <see cref="TaskList" /> property's name.
		/// </summary>
		public const string TaskListPropertyName = "TaskList";

		private DropNetClient _dropNetclient;
		private String _password = String.Empty;
		private TaskLoadingState _state = TaskLoadingState.NotLoaded;
		private String _taskList = String.Empty;
		private string _username = String.Empty;

		/// <summary>
		/// Initializes a new instance of the MainViewModel class.
		/// </summary>
		public MainViewModel()
		{
			if (IsInDesignMode)
			{
				// Code runs in Blend --> create design time data.
			}
			else
			{
				// Code runs "for real"
#if DEBUG
				Username = "hartez@gmail.com";
				Password = "23yoink42dropbox";
#endif

				LoadTasksCommand = new RelayCommand(LoadTasks, () => _state != TaskLoadingState.Loading);
			}
		}

		public RelayCommand LoadTasksCommand { get; private set; }

		public string ApplicationTitle
		{
			get { return "Todo.txt"; }
		}

		/// <summary>
		/// Gets the Username property.
		/// Changes to that property's value raise the PropertyChanged event. 
		/// </summary>
		public string Username
		{
			get { return _username; }

			set
			{
				if (_username == value)
				{
					return;
				}

				_username = value;

				// Update bindings, no broadcast
				RaisePropertyChanged(UsernamePropertyName);
			}
		}

		/// <summary>
		/// Gets the Password property.
		/// Changes to that property's value raise the PropertyChanged event. 
		/// </summary>
		public String Password
		{
			get { return _password; }

			set
			{
				if (_password == value)
				{
					return;
				}

				_password = value;

				// Update bindings, no broadcast
				RaisePropertyChanged(PasswordPropertyName);
			}
		}

		/// <summary>
		/// Gets the TaskList property.
		/// Changes to that property's value raise the PropertyChanged event. 
		/// </summary>
		public String TaskList
		{
			get { return _taskList; }

			set
			{
				if (_taskList == value)
				{
					return;
				}

				_taskList = value;

				// Update bindings, no broadcast
				RaisePropertyChanged(TaskListPropertyName);
			}
		}

		private void LoadTasks()
		{
			LoginToDropbox();
		}

		private void LoginCallback(RestResponse<UserLogin> response)
		{
			// Check response for an error

			// If response doesn't have an error, save credentials to app settings
			SaveCredentials();

			_dropNetclient.GetFileAsync("/todo/todo.txt", GotTaskFile);
		}

		private void SaveCredentials()
		{
			// Check application settings for whether to store credentials

			// Save credentials if we're supposed to
			IsolatedStorageSettings.ApplicationSettings["dropboxUsername"] = Username;
			IsolatedStorageSettings.ApplicationSettings["dropboxPassword"] = Password;
		}

		private void GotTaskFile(RestResponse response)
		{
			TaskList = response.Content;

			_state = TaskLoadingState.Loaded;
			Messenger.Default.Send(new LoadingStateChangedMessage(_state));
		}

		public void LoginToDropbox()
		{
			_state = TaskLoadingState.Loading;
			Messenger.Default.Send(new LoadingStateChangedMessage(_state));

			_dropNetclient = DropNetExtensions.CreateClient();

			bool haveCredentials =
				!String.IsNullOrEmpty(Username) && !String.IsNullOrEmpty(Password);

			if (!haveCredentials)
			{
				String usernameSetting;
				String passwordSetting = null;

				haveCredentials = IsolatedStorageSettings.ApplicationSettings.TryGetValue("dropboxUsername", out usernameSetting)
				                  && IsolatedStorageSettings.ApplicationSettings.TryGetValue("dropboxPassword", out passwordSetting);

				if (haveCredentials)
				{
					Username = usernameSetting;
					Password = passwordSetting;
				}
			}

			if (haveCredentials)
			{
				_dropNetclient.LoginAsync(Username, Password, LoginCallback);
			}
			else
			{
				Messenger.Default.Send(new NeedCredentialsMessage());
			}
		}
	}
}