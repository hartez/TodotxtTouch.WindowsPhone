using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using AgiliTrain.PhoneyTools;
using DropNet.Models;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Phone.Reactive;
using RestSharp;
using todotxtlib.net;
using TodotxtTouch.WindowsPhone.ViewModel;

namespace TodotxtTouch.WindowsPhone.Service
{
	public abstract class TaskFileService
	{
		private readonly IObservable<IEvent<TaskListChangedEventArgs>> _changeObserver;
		private readonly TaskList _taskList = new TaskList();
		private IDisposable _changeSubscription;
		private readonly DropBoxService _dropBoxService;
		protected readonly ApplicationSettings Settings;
		private TaskLoadingState _loadingState = TaskLoadingState.NotLoaded;
		private DateTime? _localLastModified;

		private bool LocalHasChanges
		{
			get
			{
				bool hasChanges;
				if(IsolatedStorageSettings.ApplicationSettings.TryGetValue(GetFileName() + "haschanges", out hasChanges))
				{
					return hasChanges;
				}

				return false;
			}
			set { IsolatedStorageSettings.ApplicationSettings[GetFileName() + "haschanges"] = value; }
		}

		protected TaskFileService(DropBoxService dropBoxService, ApplicationSettings settings)
		{
			_dropBoxService = dropBoxService;
			Settings = settings;

			_taskList.CollectionChanged += TaskListCollectionChanged;

			_changeObserver = Observable.FromEvent<TaskListChangedEventArgs>(this, "TaskListChanged");

			_dropBoxService.DropBoxServiceConnectedChanged += DropBoxServiceConnectedChanged;

			Messenger.Default.Register<ApplicationReadyMessage>(
				this, message =>
					{
						if (LoadingState == TaskLoadingState.NotLoaded)
						{
							Trace.Write(PhoneLogger.LogLevel.Debug, "State is NotLoaded for file {0}; starting sync", GetFileName());		
							Sync();
						}
					});
		}

		void DropBoxServiceConnectedChanged(object sender, DropBoxServiceAvailableChangedEventArgs e)
		{
			Sync();
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
					return appStorage.FileExists(GetFileName());
				}
			}
		}

		protected abstract String GetFilePath();
		protected abstract String GetFileName();

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
			_dropBoxService.GetMetaData(GetFilePath() + GetFileName(), metaDataCallback);
		}

		private void Sync()
		{
			if (_dropBoxService.Accessible)
			{
				if(LoadingState != TaskLoadingState.Syncing)
				{
					Trace.Write(PhoneLogger.LogLevel.Debug, "Changing state to Syncing: {0}", GetFileName());

					LoadingState = TaskLoadingState.Syncing;

					// If so, get the metadata for the remote file
					GetRemoteMetaData(metaDataResponse => Sync(metaDataResponse.Data));
				}
			}
			else if(LoadingState == TaskLoadingState.NotLoaded)
			{
				// Check for a local file
				if (!LocalFileExists)
				{
					Trace.Write(PhoneLogger.LogLevel.Debug, "Local file {0} does not exist; creating it", GetFileName());
					SaveTasks();
				}

				Trace.Write(PhoneLogger.LogLevel.Debug, "Local file {0} exists; loading it up", GetFileName());
				LoadTasks();
				PushLocal();
			}
		}

		private void Sync(MetaData data)
		{
			bool remoteExists = !String.IsNullOrEmpty(data.Name);

			if (!remoteExists)
			{
				if(LocalFileExists)
				{
					// If there's no remote file but there is a local file,
					// then we need to push the local file up
					PushLocal();
					return;
				}
				else
				{
					// No remote and no local? Then save the current task list (even if empty) as the local file
					SaveTasks();
					LoadTasks();
					return;
				}
			}

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
				if (LocalLastModified.Value.CompareTo(remoteLastModified) < 1 && !LocalHasChanges)
				{
					IsolatedStorageSettings.ApplicationSettings["LastLocalModified"] = remoteLastModified;
					UseRemoteFile(remoteLastModified);
				}
				else if (LocalLastModified.Value.CompareTo(remoteLastModified) < 0 && LocalHasChanges)
				{
					//If local.Retrieved < remote.LastUpdated and local has changes, merge (???) or maybe just upload local to conflicted file?
					Merge();
				}
				else if (LocalLastModified.Value.CompareTo(remoteLastModified) == 0 && LocalHasChanges)
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
			if (_dropBoxService.Accessible)
			{
				using (IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
				{
					using (IsolatedStorageFileStream file = appStorage.OpenFile(GetFileName(), FileMode.Open, FileAccess.Read))
					{
						var bytes = new byte[file.Length];
						file.Read(bytes, 0, (int) file.Length);

						_dropBoxService.Upload("/todo", GetFileName(), bytes, response =>
							{
								if (response.ErrorException == null)
								{
									GetRemoteMetaData(metaDataResponse =>
										{
											LocalHasChanges = false;
											LocalLastModified = metaDataResponse.Data.UTCDateModified;

											Trace.Write(PhoneLogger.LogLevel.Debug, "Changing state to Ready: {0}", GetFileName());

											LoadingState = TaskLoadingState.Ready;
										});
								}
								else
								{
									// Handle error
									Trace.Write(PhoneLogger.LogLevel.Error, response.ErrorMessage);

									LoadingState = TaskLoadingState.Ready;
								}
							});
					}
				}
			}
		}

		private void Merge()
		{
			if (_dropBoxService.Accessible)
			{
				_dropBoxService.GetFile(GetFilePath() + GetFileName(),
				                        response =>
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
				                        			Trace.Write(PhoneLogger.LogLevel.Error, response.ErrorMessage);

				                        			LoadingState = TaskLoadingState.Ready;
				                        		}
				                        	});
			}
		}

		private void LoadTasks()
		{
			using (IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
			{
				using (IsolatedStorageFileStream file = appStorage.OpenFile(GetFileName(), FileMode.Open, FileAccess.Read))
				{
					PauseChangeObserver();
					TaskList.LoadTasks(file);
					ResumeChangeObserver();

					Trace.Write(PhoneLogger.LogLevel.Debug, "Changing state to Ready: {0}", GetFileName());		

					LoadingState = TaskLoadingState.Ready;
				}
			}
		}

		private void SaveTasks()
		{
			using (IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
			{
				using (IsolatedStorageFileStream file = appStorage.OpenFile(GetFileName(), FileMode.OpenOrCreate, FileAccess.Write))
				{
					TaskList.SaveTasks(file);
				}
			}

			LocalHasChanges = true;
			Sync();
		}

		private void OverwriteWithRemoteFile(RestResponse response, DateTime remoteModifiedTime)
		{
			using (IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
			{
				using (IsolatedStorageFileStream file = appStorage.OpenFile(GetFileName(), FileMode.OpenOrCreate))
				{
					using (var writer = new StreamWriter(file))
					{
						writer.Write(response.Content);
						writer.Flush();
					}
				}
			}

			LocalLastModified = remoteModifiedTime;
			LocalHasChanges = false;
			LoadTasks();
		}

		private void UseRemoteFile(DateTime remoteModifiedTime)
		{
			_dropBoxService.GetFile(GetFilePath() + GetFileName(),
			                        response => OverwriteWithRemoteFile(response, remoteModifiedTime));
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

	public class PrimaryTaskFileService : TaskFileService
	{
		public PrimaryTaskFileService(DropBoxService dropBoxService, ApplicationSettings settings) : base(dropBoxService, settings)
		{
		}

		protected override string GetFilePath()
		{
			return Settings.TodoFilePath;
		}

		protected override string GetFileName()
		{
			return Settings.TodoFileName;
		}
	}

	public class ArchiveTaskFileService : TaskFileService
	{
		public ArchiveTaskFileService(DropBoxService dropBoxService, ApplicationSettings settings) : base(dropBoxService, settings)
		{
		}

		protected override string GetFilePath()
		{
			return Settings.ArchiveFilePath;
		}

		protected override string GetFileName()
		{
			return Settings.ArchiveFileName;
		}
	}
}