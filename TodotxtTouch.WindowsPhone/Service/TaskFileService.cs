using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
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
		private readonly IObservable<IEvent<TaskListChangedEventArgs>> _changeObserver;
		private readonly TaskList _taskList = new TaskList();
		private readonly string _taskFileName;
		private IDisposable _changeSubscription;
		private readonly DropBoxService _dropBoxService;

		private TaskLoadingState _loadingState = TaskLoadingState.NotLoaded;
		private bool _localHasChanges;
		private DateTime? _localLastModified;

		public TaskFileService(DropBoxService dropBoxService, string taskFileName)
		{
			_dropBoxService = dropBoxService;
			this._taskFileName = taskFileName;

			_taskList.CollectionChanged += TaskListCollectionChanged;

			_changeObserver = Observable.FromEvent<TaskListChangedEventArgs>(this, "TaskListChanged");

			_dropBoxService.DropBoxServiceConnectedChanged += DropBoxServiceConnectedChanged;

			Messenger.Default.Register<ApplicationReadyMessage>(
				this, (message) =>
					{
						if (LoadingState == TaskLoadingState.NotLoaded)
						{
							Debug.WriteLine("State is NotLoaded; checking for local file");

							if (LocalFileExists)
							{
								Debug.WriteLine("Local file exists; loading it up");
								LoadTasks();
							}
						}
					});
		}

		void DropBoxServiceConnectedChanged(object sender, DropBoxServiceConnectedChangedEventArgs e)
		{
			if(_dropBoxService.Connected)
			{
				// We just got connected - synchronize
				Sync();
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

		private bool LocalFileExists
		{
			get
			{
				using (IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
				{
					return appStorage.FileExists(_taskFileName);
				}
			}
		}

		private void PauseChangeObserver()
		{
			if (_changeSubscription != null)
			{
				_changeSubscription.Dispose();
			}
		}

		private void ResumeChangeObserver()
		{
			_changeSubscription = _changeObserver.Throttle(new TimeSpan(0, 0, 0, 0, 50))
				.Subscribe(e => SaveTasks());
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
			_dropBoxService.GetMetaData("/todo/" + _taskFileName, metaDataCallback);
		}

		private void Sync()
		{
			Debug.WriteLine(string.Format("Changing state to Syncing: {0}", _taskFileName));

			LoadingState = TaskLoadingState.Syncing;

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
			if (!LocalFileExists)
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
				using (IsolatedStorageFileStream file = appStorage.OpenFile(_taskFileName, FileMode.Open, FileAccess.Read))
				{
					var bytes = new byte[file.Length];
					file.Read(bytes, 0, (int) file.Length);

					_dropBoxService.Upload("/todo", _taskFileName, bytes, (response) =>
						{
							if (response.ErrorException == null)
							{
								GetRemoteMetaData((metaDataResponse) =>
									{
										_localHasChanges = false;
										LocalLastModified = metaDataResponse.Data.UTCDateModified;

										Debug.WriteLine(string.Format("Changing state to Ready: {0}", _taskFileName));

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


			_dropBoxService.GetFile("/todo/" + _taskFileName,
			                        (response) =>
			                        	{
			                        		if (response.ErrorException == null)
			                        		{
			                        			var tl = new TaskList();

			                        			using (var ms = new MemoryStream(
			                        				Encoding.GetEncoding(
			                        					response.ContentEncoding).GetBytes(response.Content)))
			                        			{
			                        				tl.LoadTasks(ms);

			                        				// Find the tasks in tl which aren't already in the 
			                        				// current tasklist
			                        				var tasksToAdd =
			                        					tl.Where(x => !TaskList.Any(y => x.ToString() == y.ToString()));
			                        				foreach (var task in tasksToAdd)
			                        				{
			                        					TaskList.Add(task);
			                        				}

			                        				PushLocal();
			                        			}
			                        		}
			                        		else
			                        		{
			                        			// Handle error
			                        		}
			                        	});
		}

		private void LoadTasks()
		{
			using (IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
			{
				using (IsolatedStorageFileStream file = appStorage.OpenFile(_taskFileName, FileMode.Open, FileAccess.Read))
				{
					PauseChangeObserver();
					TaskList.LoadTasks(file);
					ResumeChangeObserver();

					Debug.WriteLine(string.Format("Changing state to Ready: {0}", _taskFileName));

					LoadingState = TaskLoadingState.Ready;
				}
			}
		}

		private void SaveTasks()
		{
			using (IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
			{
				using (IsolatedStorageFileStream file = appStorage.OpenFile(_taskFileName, FileMode.OpenOrCreate, FileAccess.Write))
				{
					TaskList.SaveTasks(file);

					// TODO Make local has changes get tombstoned so we can sync anything we missed
					// last time we got deactivated
					_localHasChanges = true;
				}
			}

			Sync();
		}

		private void OverwriteWithRemoteFile(RestResponse response, DateTime remoteModifiedTime)
		{
			using (IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
			{
				using (IsolatedStorageFileStream file = appStorage.OpenFile(_taskFileName, FileMode.OpenOrCreate))
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
			_dropBoxService.GetFile("/todo/" + _taskFileName,
			                        (response) => OverwriteWithRemoteFile(response, remoteModifiedTime));
		}

		#region Events

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

		#endregion
	}
}