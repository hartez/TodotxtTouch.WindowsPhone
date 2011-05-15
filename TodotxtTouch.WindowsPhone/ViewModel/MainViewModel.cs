using System;
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

		/// <summary>
		/// The <see cref="LoadingState" /> property's name.
		/// </summary>
		public const string LoadingStatePropertyName = "LoadingState";

		/// <summary>
		/// The <see cref="SelectedTask" /> property's name.
		/// </summary>
		public const string SelectedTaskPropertyName = "SelectedTask";

		private readonly DropBoxCredentialsViewModel _dropBoxCredentials;

		private DropNetClient _dropNetclient;
		private readonly TaskList _taskList = new TaskList();

		private TaskLoadingState _loadingState = TaskLoadingState.NotLoaded;
		private bool _localHasChanges;
		private DateTime? _localLastModified;

		private Task _selectedTask;
		private string todoFileName = "testingtodo.txt";

		/// <summary>
		/// Initializes a new instance of the MainViewModel class.
		/// </summary>
		public MainViewModel(DropBoxCredentialsViewModel dropBoxCredentialsViewModel)
		{
			_dropBoxCredentials = dropBoxCredentialsViewModel;

			if (IsInDesignMode)
			{
				// Code runs in Blend --> create design time data.
			}
			else
			{
				// Code runs "for real"
				
				
				LoadTasksCommand = new RelayCommand(Sync, () => LoadingState == TaskLoadingState.NotLoaded);
				ViewTaskDetailsCommand = new RelayCommand(ViewTask);
			}
		}

		/// <summary>
		/// Gets the LoadingState property.
		/// This property's value is broadcasted by the Messenger's default instance when it changes.
		/// </summary>
		public TaskLoadingState LoadingState
		{
			get { return _loadingState; }

			set
			{
				if (_loadingState == value)
				{
					return;
				}

				TaskLoadingState oldValue = _loadingState;
				_loadingState = value;

				// Update bindings and broadcast change using GalaSoft.MvvmLight.Messenging
				RaisePropertyChanged(LoadingStatePropertyName, oldValue, value, true);
			}
		}

		/// <summary>
		/// Gets the SelectedTask property.
		/// Changes to that property's value raise the PropertyChanged event. 
		/// </summary>
		public Task SelectedTask
		{
			get { return _selectedTask; }

			set
			{
				if (_selectedTask == value)
				{
					return;
				}

				_selectedTask = value;

				// Update bindings, no broadcast
				RaisePropertyChanged(SelectedTaskPropertyName);
			}
		}

		public RelayCommand LoadTasksCommand { get; private set; }

		public RelayCommand ViewTaskDetailsCommand { get; private set; }

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

		private DateTime? LocalLastModified
		{
			get
			{
				if (_localLastModified == null)
				{
					DateTime? llm;
					if (IsolatedStorageSettings.ApplicationSettings.TryGetValue("LastLocalModified", out llm))
					{
						_localLastModified = llm;
					}
				}

				return _localLastModified;
			}
			set
			{
				_localLastModified = value;
				IsolatedStorageSettings.ApplicationSettings["LastLocalModified"] = _localLastModified;
			}
		}

		private bool HaveLocalFile
		{
			get
			{
				using (IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
				{
					return appStorage.FileExists(todoFileName);
				}
			}
		}

		private void ViewTask()
		{
			Messenger.Default.Send(new ViewTaskMessage());
		}

		private void GotRemoteMetadata(RestResponse<MetaData> obj)
		{
			Sync(obj.Data);
		}

		private void GetRemoteMetaData()
		{
			if (!_dropBoxCredentials.IsAuthenticated)
			{
				LoginToDropbox(GetRemoteMetaData);
			}
			else
			{
				_dropNetclient.GetMetaDataAsync("/todo/" + todoFileName, GotRemoteMetadata);
			}
		}

		private void Sync()
		{
			if (_dropNetclient == null && _dropBoxCredentials.IsAuthenticated)
			{
				_dropNetclient = DropNetExtensions.CreateClient(_dropBoxCredentials.Token, _dropBoxCredentials.Secret);
			}

			// Check to see if we have a data connection
			// and whether dropbox is considered accessible
			// If so, get the metadata for the remote file
			GetRemoteMetaData();

			// If not, do nothing right now
		}

		private void Sync(MetaData data)
		{
			DateTime remoteLastModified = data.UTCDateModified;

			// See if we have a local task file
			if (!HaveLocalFile)
			{
				// We have no local file - just make the remote file the local file
				UseRemoteFile(remoteLastModified);
				return;
			}

			// Use the metadata to make a decision about whether to 
			// get/merge the remote file
			if (LocalLastModified.HasValue)
			{
				//	If local.Retrieved <= remote.LastUpdated and local has no changes, replace local with remote (local.Retrieved = remote.LastUpdated)
				if (LocalLastModified.Value.CompareTo(remoteLastModified) < 1 && !_localHasChanges)
				{
					IsolatedStorageSettings.ApplicationSettings["LastLocalModified"] = remoteLastModified;
					UseRemoteFile(remoteLastModified);
				}
				else if (LocalLastModified.Value.CompareTo(remoteLastModified) < 0 && _localHasChanges)
				{
					//If local.Retrieved < remote.LastUpdated and local has changes, merge (???) or maybe just upload local to conflicted file?
					Merge();
				}
				else if (LocalLastModified.Value.CompareTo(remoteLastModified) == 0 && _localHasChanges)
				{
					//If local.Retrieved == remote.LastUpdate and local has changes, upload local
					PushLocal();
				}
			}
		}

		private void PushLocal()
		{
			// Upload the local version, then get metadata to update local last modified
		}

		private void Merge()
		{
			// Get the remote, merge, push local, get local last modified
		}

		private void LoadTasks()
		{
			using (IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
			{
				using (IsolatedStorageFileStream file = appStorage.OpenFile(todoFileName, FileMode.Open, FileAccess.Read))
				{
					TaskList.LoadTasks(file);
					LoadingState = TaskLoadingState.Loaded;
				}
			}
		}

		private void OverwriteWithRemoteFile(RestResponse response, DateTime remoteModifiedTime)
		{
			using (IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
			{
				using (IsolatedStorageFileStream file = appStorage.OpenFile(todoFileName, FileMode.OpenOrCreate))
				{
					using (var writer = new StreamWriter(file))
					{
						writer.Write(response.Content);
						writer.Flush();
					}
				}
			}

			LocalLastModified = remoteModifiedTime;
			LoadTasks();
		}

		private void UseRemoteFile(DateTime remoteModifiedTime)
		{
			if (!_dropBoxCredentials.IsAuthenticated)
			{
				LoginToDropbox(() => UseRemoteFile(remoteModifiedTime));
			}
			else
			{
				_dropNetclient.GetFileAsync("/todo/" + todoFileName,
					(response) => OverwriteWithRemoteFile(response, remoteModifiedTime));
			}
		}

		private void LoginCallback(RestResponse<UserLogin> response)
		{
			// Check response for an error

	
			_dropBoxCredentials.Secret = response.Data.Secret;
			_dropBoxCredentials.Token = response.Data.Token;
		}

		public void LoginToDropbox(Action loginCallbackAction)
		{
			LoadingState = TaskLoadingState.Loading;

			if (_dropBoxCredentials.HasLoginCredentials)
			{
				_dropNetclient = DropNetExtensions.CreateClient();
				_dropNetclient.LoginAsync(_dropBoxCredentials.Username, _dropBoxCredentials.Password,
				                          response =>
				                          	{
				                          		LoginCallback(response);
				                          		loginCallbackAction();
				                          	});
			}
		}

	}
}