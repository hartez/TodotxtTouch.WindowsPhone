using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using DropNet;
using DropNet.Models;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Phone.Reactive;
using RestSharp;
using todotxtlib.net;
using TodotxtTouch.WindowsPhone.ViewModel;

namespace TodotxtTouch.WindowsPhone.Service
{
	public class TaskFileService
	{
		private readonly DropBoxCredentialsViewModel _dropBoxCredentials;
		private readonly TaskList _taskList = new TaskList();
		private readonly string taskFileName;

		private DropNetClient _dropNetclient;
		private TaskLoadingState _loadingState = TaskLoadingState.NotLoaded;
		private bool _localHasChanges;
		private DateTime? _localLastModified;
		private IObservable<IEvent<TaskListChangedEventArgs>> _changeObserver;
		private IDisposable _changeSubscription;

		private void DisableChangeObserver()
		{
			if (_changeSubscription != null)
			{
				_changeSubscription.Dispose();
			}
		}

		private void EnableChangeObserver()
		{
			_changeSubscription = _changeObserver.Throttle(new TimeSpan(0, 0, 0, 0, 100))
				.Subscribe(e => SaveTasks());
		}

		public TaskFileService(DropBoxCredentialsViewModel dropBoxCredentialsViewModel, string taskFileName)
		{
			_dropBoxCredentials = dropBoxCredentialsViewModel;
			this.taskFileName = taskFileName;

			_taskList.CollectionChanged += TaskListCollectionChanged;

			_changeObserver = Observable.FromEvent<TaskListChangedEventArgs>(this, "TaskListChanged");

			Messenger.Default.Register<CredentialsUpdatedMessage>(
				this, message => Sync());

			Messenger.Default.Register<ApplicationReadyMessage>(
				this, (message) =>
					{
						if (LoadingState == TaskLoadingState.NotLoaded)
						{
							if (HaveLocalFile)
							{
								LoadTasks();
							}

							Sync();
						}
					});
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
				_loadingState = value;
				InvokeLoadingStateChanged(new LoadingStateChangedEventArgs(_loadingState));
			}
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
					return appStorage.FileExists(taskFileName);
				}
			}
		}

		private void TaskListCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Remove)
			{
				foreach (Task item in e.OldItems)
				{
					//Removed items
					item.PropertyChanged -= TaskPropertyChanged;
				}
			}
			else if (e.Action == NotifyCollectionChangedAction.Add)
			{
				foreach (Task item in e.NewItems)
				{
					//Added items
					item.PropertyChanged += TaskPropertyChanged;
				}
			}

			InvokeTaskListChanged(new TaskListChangedEventArgs());
		}

		public event EventHandler<LoadingStateChangedEventArgs> LoadingStateChanged;
		public event EventHandler<TaskListChangedEventArgs> TaskListChanged;

		public void InvokeTaskListChanged(TaskListChangedEventArgs e)
		{
			EventHandler<TaskListChangedEventArgs> handler = TaskListChanged;
			if (handler != null)
			{
				handler(this, e);
			}
		}

		public void InvokeLoadingStateChanged(LoadingStateChangedEventArgs e)
		{
			EventHandler<LoadingStateChangedEventArgs> handler = LoadingStateChanged;
			if (handler != null)
			{
				handler(this, e);
			}
		}

		private void TaskPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			InvokeTaskListChanged(new TaskListChangedEventArgs());
		}

		public void UpdateTask(Task task, Task oldTask)
		{
			int index = TaskList.IndexOf(oldTask);
			TaskList[index].UpdateTo(task);
		}

		private void GetRemoteMetaData(Action<RestResponse<MetaData>> metaDataCallback)
		{
			if (!_dropBoxCredentials.IsAuthenticated)
			{
				LoginToDropbox(() => GetRemoteMetaData(metaDataCallback));
			}
			else
			{
				_dropNetclient.GetMetaDataAsync("/todo/" + taskFileName, metaDataCallback);
			}
		}

		private void Sync()
		{
			LoadingState = TaskLoadingState.Syncing;

			if (_dropNetclient == null && _dropBoxCredentials.IsAuthenticated)
			{
				_dropNetclient = DropNetExtensions.CreateClient(_dropBoxCredentials.Token, _dropBoxCredentials.Secret);
			}

			// TODO Check to see if we have a data connection
			// TODO and whether dropbox is considered accessible
			// If so, get the metadata for the remote file
			GetRemoteMetaData((metaDataResponse) => Sync(metaDataResponse.Data));

			// If not, do nothing right now
		}

		private void Sync(MetaData data)
		{
			// TODO - Need to handle the possibility of no remote file existing
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
				using (IsolatedStorageFileStream file = appStorage.OpenFile(taskFileName, FileMode.Open, FileAccess.Read))
				{
					var bytes = new byte[file.Length];
					file.Read(bytes, 0, (int) file.Length);

					// Upload the local version, then get metadata to update local last modified
					_dropNetclient.UploadFileAsync("/todo", taskFileName, bytes, (response) =>
						{
							if (response.ErrorException == null)
							{
								GetRemoteMetaData((metaDataResponse) =>
									{
										_localHasChanges = false;
										LocalLastModified = metaDataResponse.Data.UTCDateModified;
										LoadingState = TaskLoadingState.Ready;
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
				using (IsolatedStorageFileStream file = appStorage.OpenFile(taskFileName, FileMode.Open, FileAccess.Read))
				{
					DisableChangeObserver();
					TaskList.LoadTasks(file);
					EnableChangeObserver();
					LoadingState = TaskLoadingState.Ready;
				}
			}
		}

		private void SaveTasks()
		{
			using (IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
			{
				using (IsolatedStorageFileStream file = appStorage.OpenFile(taskFileName, FileMode.OpenOrCreate, FileAccess.Write))
				{
					TaskList.SaveTasks(file);
					_localHasChanges = true;
				}
			}

			Sync();
		}

		private void OverwriteWithRemoteFile(RestResponse response, DateTime remoteModifiedTime)
		{
			using (IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
			{
				using (IsolatedStorageFileStream file = appStorage.OpenFile(taskFileName, FileMode.OpenOrCreate))
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
				_dropNetclient.GetFileAsync("/todo/" + taskFileName,
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