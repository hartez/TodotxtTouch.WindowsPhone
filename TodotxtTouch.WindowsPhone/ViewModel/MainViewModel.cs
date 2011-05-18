using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using DropNet;
using DropNet.Models;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Phone.Reactive;
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
		#region Property Names
		
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

		/// <summary>
		/// The <see cref="SelectedTaskDraft" /> property's name.
		/// </summary>
		public const string SelectedTaskDraftPropertyName = "SelectedTaskDraft";

		#endregion

		#region Backing fields
		private readonly DropBoxCredentialsViewModel _dropBoxCredentials;

		private readonly TaskList _taskList = new TaskList();
		private ObservableCollection<string> _availablePriorities = new ObservableCollection<string>();
		private DropNetClient _dropNetclient;

		private TaskLoadingState _loadingState = TaskLoadingState.NotLoaded;
		private bool _localHasChanges;
		private DateTime? _localLastModified;

		private Task _selectedTask;
		private string todoFileName = "testingtodo.txt";
		private Task _selectedTaskDraft;

		#endregion

		/// <summary>
		/// Initializes a new instance of the MainViewModel class.
		/// </summary>
		public MainViewModel(DropBoxCredentialsViewModel dropBoxCredentialsViewModel)
		{
			_dropBoxCredentials = dropBoxCredentialsViewModel;

			if (IsInDesignMode)
			{
				// Code runs in Blend --> create design time data.
				Observable.Range(65, 26).Select(n => ((char) n).ToString()).Subscribe(p => _availablePriorities.Add(p));

				_taskList.Add(new Task("A", null, null, "This is a designer task"));
				_taskList.Add(new Task("", null, null, "This is a designer task2"));
				_taskList.Add(new Task("", null, null, "This is a designer task3"));
				var b = new Task("B", null, null, "This is a designer task4");
				b.ToggleCompleted();
				_taskList.Add(b);
				_taskList.Add(new Task("C", null, null, "This is a designer task5"));

				_selectedTask = _taskList[3];
			}
			else
			{
				// Code runs "for real"
				WireUpCommands();

				Messenger.Default.Register<CredentialsUpdatedMessage>(
					this, (message) => Sync());
			}
		}

		public ObservableCollection<String> AvailablePriorities
		{
			get { return _availablePriorities; }
			private set { _availablePriorities = value; }
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

		/// <summary>
		/// Gets the SelectedTaskDraft property.
		/// Changes to that property's value raise the PropertyChanged event. 
		/// </summary>
		public Task SelectedTaskDraft
		{
			get { return _selectedTaskDraft; }

			set
			{
				if (_selectedTaskDraft == value)
				{
					return;
				}

				_selectedTaskDraft = value;

				// Update bindings, no broadcast
				RaisePropertyChanged(SelectedTaskDraftPropertyName);
			}
		}

		#region Commands

		private void WireUpCommands()
		{
			LoadTasksCommand = new RelayCommand(Sync, () => LoadingState == TaskLoadingState.NotLoaded);
			
			ViewTaskDetailsCommand = new RelayCommand(ViewTask, () =>
				LoadingState == TaskLoadingState.Loaded 
				&& SelectedTask != null);

			AddTaskCommand = new RelayCommand(AddTask, () => LoadingState == TaskLoadingState.Loaded);

			SaveCurrentTaskCommand = new RelayCommand(SaveCurrentTask, () => LoadingState == TaskLoadingState.Loaded
				&& SelectedTaskDraft != null);
		}

		public RelayCommand LoadTasksCommand { get; private set; }

		public RelayCommand ViewTaskDetailsCommand { get; private set; }

		public RelayCommand SaveCurrentTaskCommand { get; private set; }

		public RelayCommand AddTaskCommand { get; private set; }
			 
		private void AddTask()
		{
			SelectedTask = null;

			SelectedTaskDraft = new Task(String.Empty, null, null, String.Empty);

			UpdateAvailablePriorities();

			Messenger.Default.Send(new ViewTaskMessage());
		}

		private void ViewTask()
		{
			SelectedTaskDraft = SelectedTask;

			UpdateAvailablePriorities();

			Messenger.Default.Send(new ViewTaskMessage());
		}

		private void SaveCurrentTask()
		{
			LoadingState = TaskLoadingState.Saving;

			if (SelectedTask != null)
			{
				var index = TaskList.IndexOf(SelectedTask);
				TaskList[index] = SelectedTaskDraft;
			}
			else
			{
				TaskList.Add(SelectedTaskDraft);
			}

			_localHasChanges = true;

			SaveTasks();
			Sync();
		}

		#endregion

		/// <summary>
		/// Gets the ApplicationTitle property.
		/// </summary>
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

		private void UpdateAvailablePriorities()
		{
			_availablePriorities.Clear();
			_availablePriorities.Add("");

			IEnumerable<String> prioritiesInUse =
				(from t in _taskList
				 where t.IsPriority && t.Priority != SelectedTaskDraft.Priority
				 orderby t.Priority
				 select t.Priority).Distinct();

			// Generate the possible priorities, then skip over the ones that are already in use
			Observable.Range(65, 26).Select(n => ((char) n).ToString()).SkipWhile(c => prioritiesInUse.Contains(c))
				.Subscribe((priority) => _availablePriorities.Add(priority));
		}

		private void GetRemoteMetaData(Action<RestResponse<MetaData>> metaDataCallback)
		{
			if (!_dropBoxCredentials.IsAuthenticated)
			{
				LoginToDropbox(() => GetRemoteMetaData(metaDataCallback));
			}
			else
			{
				_dropNetclient.GetMetaDataAsync("/todo/" + todoFileName, metaDataCallback);
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
			GetRemoteMetaData((metaDataResponse) => Sync(metaDataResponse.Data));

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
			else
			{
				UseRemoteFile(remoteLastModified);
			}
		}

		private void PushLocal()
		{
			using (IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
			{
				using (IsolatedStorageFileStream file = appStorage.OpenFile(todoFileName, FileMode.Open, FileAccess.Read))
				{
					var bytes = new byte[file.Length];
					file.Read(bytes, 0, (int)file.Length);

					// Upload the local version, then get metadata to update local last modified
					_dropNetclient.UploadFileAsync("/todo", todoFileName, bytes, (response) =>
						{
							if (response.ErrorException == null)
							{
								GetRemoteMetaData((metaDataResponse) =>
									{
										_localHasChanges = false;
										LocalLastModified = metaDataResponse.Data.UTCDateModified;
										LoadingState = TaskLoadingState.Loaded;
									});
							}

							// Handle error
						});
				}
			}
		}

		private void Merge()
		{
			// Get the remote, merge, push local
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

		private void SaveTasks()
		{
			using (IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
			{
				using (IsolatedStorageFileStream file = appStorage.OpenFile(todoFileName, FileMode.Open, FileAccess.Write))
				{
					TaskList.SaveTasks(file);
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
			_localHasChanges = false;
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