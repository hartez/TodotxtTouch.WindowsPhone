using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using System.Windows.Resources;
using DropNet;
using DropNet.Models;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
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

		private readonly Dictionary<string, string> _apikeys = new Dictionary<string, string>();
		private DropNetClient _dropNetclient;
		private String _password = String.Empty;
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
				_apikeys = LoadApiKeysFromFile();
				LoadTasksCommand = new RelayCommand(LoadTasks);
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

				string oldValue = _taskList;
				_taskList = value;

				// Update bindings, no broadcast
				RaisePropertyChanged(TaskListPropertyName);
			}
		}

		private bool _loggedIn = false;

		private void LoadTasks()
		{
			if (!_loggedIn)
			{
				LoginToDropbox();
			}
		}

		private void LoginCallback(RestResponse<UserLogin> response)
		{
			// Check response for an error

			// If response doesn't have an error, save credentials to app settings
			SaveCredentials();

			

			_dropNetclient.GetMetaDataAsync("todo", GetMetaDataCallBack);

			Messenger.Default.Send(new LoadingStateChangedMessage(TaskLoadingState.Loaded));
		}

		private void SaveCredentials()
		{
			// Check application settings for whether to store credentials

			// Save credentials if we're supposed to
		}

		private void GetMetaDataCallBack(RestResponse<MetaData> metaData)
		{
			TaskList = metaData.Data.Contents.Aggregate(String.Empty,
			                                            (list, data) =>
			                                            list + (list.Length > 0 ? ", " : "") + data.Name);
		}

		public void LoginToDropbox()
		{
			Messenger.Default.Send(new LoadingStateChangedMessage(TaskLoadingState.Loading));

			_dropNetclient = new DropNetClient(_apikeys["dropboxkey"], _apikeys["dropboxsecret"]);

			bool haveCredentials =
				!String.IsNullOrEmpty(Username) && !String.IsNullOrEmpty(Password);

			if(!haveCredentials)
			{
				String usernameSetting = null;
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
				// TODO Replace this with LoadingState 
				_loggedIn = true;
				_dropNetclient.LoginAsync(Username, Password, LoginCallback);
			}
			else
			{
				Messenger.Default.Send(new NeedCredentialsMessage());
			}
		}

		private Dictionary<string, string> LoadApiKeysFromFile()
		{
			StreamResourceInfo apikeysResource =
				Application.GetResourceStream(new Uri("/TodotxtTouch.WindowsPhone;component/apikeys.txt", UriKind.Relative));

			var sr = new StreamReader(apikeysResource.Stream);

			string keys = sr.ReadToEnd();

			JsonSerializer serializer = JsonSerializer.Create(new JsonSerializerSettings());

			var reader = new JsonTextReader(new StringReader(keys));

			return serializer.Deserialize<Dictionary<string, string>>(reader);
		}
	}
}