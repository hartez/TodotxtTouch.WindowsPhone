using System.IO;
using System.IO.IsolatedStorage;
using DropNet;
using DropNet.Models;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using RestSharp;
using todotxtlib.net;

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
		/// The <see cref="TaskList" /> property's name.
		/// </summary>
		public const string TaskListPropertyName = "TaskList";

		private readonly ApplicationSettingsViewModel _appSettings;
		private readonly TaskList _taskList = new TaskList();

		private DropNetClient _dropNetclient;

		private TaskLoadingState _state = TaskLoadingState.NotLoaded;

		/// <summary>
		/// Initializes a new instance of the MainViewModel class.
		/// </summary>
		public MainViewModel(ApplicationSettingsViewModel applicationSettingsViewModel)
		{
			_appSettings = applicationSettingsViewModel;

			if (IsInDesignMode)
			{
				// Code runs in Blend --> create design time data.
			}
			else
			{
				// Code runs "for real"
				LoadTasksCommand = new RelayCommand(LoadTasks, () => _state != TaskLoadingState.Loading);
			}
		}

		public RelayCommand LoadTasksCommand { get; private set; }

		public string ApplicationTitle
		{
			get { return "Todo.txt"; }
		}

		/// <summary>
		/// Gets the TaskList property.
		/// Changes to that property's value raise the PropertyChanged event. 
		/// </summary>
		public TaskList TaskList
		{
			get { return _taskList; }
		}

		private void LoadTasks()
		{
			LoginToDropbox();
		}

		private void LoginCallback(RestResponse<UserLogin> response)
		{
			// Check response for an error
			_dropNetclient.GetFileAsync("/todo/testingtodo.txt", GotTaskFile);
		}

		private void GotTaskFile(RestResponse response)
		{
			using (IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
			{
				using (IsolatedStorageFileStream file = appStorage.OpenFile("todo.txt", FileMode.OpenOrCreate))
				{
					using (var writer = new StreamWriter(file))
					{
						writer.Write(response.Content);
						writer.Flush();
					}
				}

				using (IsolatedStorageFileStream file = appStorage.OpenFile("todo.txt", FileMode.Open, FileAccess.Read))
				{
					TaskList.LoadTasks(file);
				}
			}

			_state = TaskLoadingState.Loaded;
			Messenger.Default.Send(new LoadingStateChangedMessage(_state));
		}

		public void LoginToDropbox()
		{
			_state = TaskLoadingState.Loading;
			Messenger.Default.Send(new LoadingStateChangedMessage(_state));

			_dropNetclient = DropNetExtensions.CreateClient();

			if (_appSettings.HasCredentials)
			{
				_dropNetclient.LoginAsync(_appSettings.Username, _appSettings.Password, LoginCallback);
			}
		}
	}
}