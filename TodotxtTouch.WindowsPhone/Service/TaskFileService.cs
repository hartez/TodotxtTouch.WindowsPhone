using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using AgiliTrain.PhoneyTools;
using DropNet.Exceptions;
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
		protected readonly ApplicationSettings Settings;
		private readonly IObservable<IEvent<TaskListChangedEventArgs>> _changeObserver;
		private readonly DropBoxService _dropBoxService;
		private readonly TaskList _taskList = new TaskList();
		private IDisposable _changeSubscription;
		private TaskLoadingState _loadingState = TaskLoadingState.Ready;
		private DateTime? _localLastModified;

		protected TaskFileService(DropBoxService dropBoxService, ApplicationSettings settings)
		{
			_dropBoxService = dropBoxService;
			Settings = settings;

			_taskList.CollectionChanged += TaskListCollectionChanged;

			_changeObserver = Observable.FromEvent<TaskListChangedEventArgs>(this, "TaskListChanged");

			Messenger.Default.Register<ApplicationReadyMessage>(
				this, message =>
					{
							Trace.Write(PhoneLogger.LogLevel.Debug, "State is NotLoaded for file {0}; loading...", GetFileName());
							if (!LocalFileExists)
							{
								Trace.Write(PhoneLogger.LogLevel.Debug, "Local file {0} does not exist; creating it", GetFileName());
								SaveTasks();
							}

							Trace.Write(PhoneLogger.LogLevel.Debug, "Local file {0} exists; loading it up", GetFileName());
							LoadTasks();
					});
		}

		private bool LocalHasChanges
		{
			get
			{
				bool hasChanges;
				if (IsolatedStorageSettings.ApplicationSettings.TryGetValue(GetFileName() + "haschanges", out hasChanges))
				{
					return hasChanges;
				}

				return false;
			}
			set { IsolatedStorageSettings.ApplicationSettings[GetFileName() + "haschanges"] = value; }
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

		private String LocalLastModifiedPropertyName
		{
			get { return GetFileName() + "LocalLastModified"; }
		}

		private DateTime? LocalLastSynced
		{
			get
			{
				if (_localLastModified == null)
				{
					DateTime? llm;
					if (IsolatedStorageSettings.ApplicationSettings.TryGetValue(LocalLastModifiedPropertyName, out llm))
					{
						_localLastModified = llm;
					}
				}

				return _localLastModified;
			}
			set
			{
				_localLastModified = value;
				IsolatedStorageSettings.ApplicationSettings[LocalLastModifiedPropertyName] = _localLastModified;
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

		private String FullPath
		{
			get { return GetFilePath() + "/" + GetFileName(); }
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

		private void GetRemoteMetaData(Action<MetaData> success, Action<DropboxException> failure)
		{
			_dropBoxService.GetMetaData(FullPath, success, failure);
		}

		public void Sync()
		{
			if (_dropBoxService.Accessible)
			{
				if (LoadingState != TaskLoadingState.Syncing)
				{
					Trace.Write(PhoneLogger.LogLevel.Debug, "Changing state to Syncing: {0}", GetFileName());

					LoadingState = TaskLoadingState.Syncing;

					// Get the metadata for the remote file
					GetRemoteMetaData(Sync,
					                  exception =>
					                  	{
					                  		Trace.Write(PhoneLogger.LogLevel.Error, exception.Message);
					                  		Sync(null);
					                  	}
						);
				}
			}
			else
			{
				_dropBoxService.Connect(Sync);
			}
		}

		private void Sync(MetaData data)
		{
			bool remoteExists = data != null && !String.IsNullOrEmpty(data.Name);

			if (!remoteExists)
			{
				if (LocalFileExists)
				{
					// If there's no remote file but there is a local file,
					// then we need to push the local file up

					Trace.Write(PhoneLogger.LogLevel.Debug,
					            "No remote file {0} exists, but we have a local version. Pushing it up.", GetFileName());

					PushLocal();
					return;
				}

				// No remote and no local? Then save the current task list (even if empty) as the local file

				Trace.Write(PhoneLogger.LogLevel.Debug,
				            "No remote file or local file named {0}; Saving local then loading tasks", GetFileName());

				SaveTasks();
				LoadTasks();
				return;
			}

			DateTime remoteLastModified = data.UTCDateModified;

			Trace.Write(PhoneLogger.LogLevel.Debug,
			            "Dropbox version of {0} last modified time is {1}", GetFileName(), remoteLastModified);

			// See if we have a local task file
			if (!LocalFileExists)
			{
				// We have no local file - just make the remote file the local file
				Trace.Write(PhoneLogger.LogLevel.Debug,
				            "No local version of {0}", GetFileName());
				UseRemoteFile(remoteLastModified);
				return;
			}

			// Use the metadata to make a decision about whether to 
			// get/merge the remote file
			if (LocalLastSynced.HasValue)
			{
				Trace.Write(PhoneLogger.LogLevel.Debug,
				            "We have a local version of {0} last modified at {1}", GetFileName(), LocalLastSynced);

				if (LocalLastSynced.Value.CompareTo(remoteLastModified) == 0 && !LocalHasChanges)
				{
					Trace.Write(PhoneLogger.LogLevel.Debug,
					            "{0}: No local changes and the last time it was retrieved is the same as the last time the Dropbox file was modified (so don't do anything)",
					            GetFileName());
					if (TaskList.Count == 0)
					{
						// We might be coming back from an error state and not have the local file loaded yet
						LoadTasks();
					}
					else
					{
						LoadingState = TaskLoadingState.Ready;
					}
				}
				else if (LocalLastSynced.Value.CompareTo(remoteLastModified) < 0 && !LocalHasChanges)
				{
					//	If local.Retrieved < remote.LastUpdated and local has no changes, replace local with remote (local.Retrieved = remote.LastUpdated)
					Trace.Write(PhoneLogger.LogLevel.Debug,
					            "{0}: No local changes and the last time it was retrieved is earlier than the last time the Dropbox file was modified",
					            GetFileName());

					IsolatedStorageSettings.ApplicationSettings["LastLocalModified"] = remoteLastModified;
					UseRemoteFile(remoteLastModified);
				}
				else if (LocalLastSynced.Value.CompareTo(remoteLastModified) < 0 && LocalHasChanges)
				{
					//If local.Retrieved < remote.LastUpdated and local has changes, merge (???) or maybe just upload local to conflicted file?
					Trace.Write(PhoneLogger.LogLevel.Debug,
					            "{0}: The dropbox file has changed since last time we retrieved it and the local one has changes",
					            GetFileName());

					Merge();
				}
				else if (LocalLastSynced.Value.CompareTo(remoteLastModified) == 0 && LocalHasChanges)
				{
					//If local.Retrieved == remote.LastUpdate and local has changes, upload local

					Trace.Write(PhoneLogger.LogLevel.Debug,
					            "{0}: Dropbox file hasn't changed since last time we retrieved it, and the local one has changes",
					            GetFileName());

					if (TaskList.Count == 0)
					{
						// We might be coming back from an error state and not have the local file loaded yet
						LoadTasks();
					}

					PushLocal();
				}
			}
			else
			{
				Trace.Write(PhoneLogger.LogLevel.Debug,
				            "We have a local version of {0}, but no idea when it was last modified", GetFileName());

				// The only reason for this to happen would be that a local file was created before any 
				// synchronization was done. So we should merge any local stuff with whatever is in the remote
				Merge();
			}
		}

		/// <summary>
		/// Reads data from a stream until the end is reached. The
		/// data is returned as a byte array. An IOException is
		/// thrown if any of the underlying IO calls fail.
		/// </summary>
		/// <param name="stream">The stream to read data from</param>
		/// <param name="initialLength">The initial buffer length</param>
		public static byte[] ReadFully(Stream stream, int initialLength)
		{
			// If we've been passed an unhelpful initial length, just
			// use 32K.
			if (initialLength < 1)
			{
				initialLength = 32768;
			}

			byte[] buffer = new byte[initialLength];
			int read = 0;

			int chunk;
			while ((chunk = stream.Read(buffer, read, buffer.Length - read)) > 0)
			{
				read += chunk;

				// If we've reached the end of our buffer, check to see if there's
				// any more information
				if (read == buffer.Length)
				{
					int nextByte = stream.ReadByte();

					// End of stream? If so, we're done
					if (nextByte == -1)
					{
						return buffer;
					}

					// Nope. Resize the buffer, put in the byte we've just
					// read, and continue
					byte[] newBuffer = new byte[buffer.Length * 2];
					Array.Copy(buffer, newBuffer, buffer.Length);
					newBuffer[read] = (byte)nextByte;
					buffer = newBuffer;
					read++;
				}
			}
			// Buffer is now too big. Shrink it.
			byte[] ret = new byte[read];
			Array.Copy(buffer, ret, read);
			return ret;
		}

		private static byte[] ReadLocalFile(string fileName)
		{
			using (IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
			{
				using (IsolatedStorageFileStream file = appStorage.OpenFile(fileName, FileMode.Open, FileAccess.Read))
				{
					return ReadFully(file, 0);
				}
			}
		}

		private void PushLocal()
		{
			if (_dropBoxService.Accessible)
			{
				Trace.Write(PhoneLogger.LogLevel.Debug,
				            "Pushing up local version of {0}", GetFileName());

				var localFile = GetFileName();

				byte[] bytes = ReadLocalFile(localFile);

				_dropBoxService.Upload(GetFilePath(), localFile, bytes, response => GetRemoteMetaData(metaDataResponse =>
					{
						LocalHasChanges = false;
						LocalLastSynced = metaDataResponse.UTCDateModified;

						Trace.Write(PhoneLogger.LogLevel.Debug, "Changing state to Ready: {0}", localFile);

						LoadingState = TaskLoadingState.Ready;
					}, LogExceptionAndReadyUp),
				                       LogExceptionAndReadyUp
					);
			}
		}

		private void LogExceptionAndReadyUp(Exception ex)
		{
			Trace.Write(PhoneLogger.LogLevel.Error, ex.Message);

			LoadingState = TaskLoadingState.Ready;
		}

		private void Merge()
		{
			if (_dropBoxService.Accessible)
			{
				Trace.Write(PhoneLogger.LogLevel.Debug,
				            "Going to attempt to merge the files");

				_dropBoxService.GetFile(FullPath,
				                        response =>
				                        	{
				                        		var tl = new TaskList();

				                        		using (var ms = new MemoryStream(
				                        			Encoding.UTF8.GetBytes(response.Content)))
				                        		{
				                        			tl.LoadTasks(ms);

				                        			// Find the tasks in tl which aren't already in the 
				                        			// current tasklist
				                        			IEnumerable<Task> tasksToAdd =
				                        				tl.Where(x => !TaskList.Any(y => x.ToString() == y.ToString()));
				                        			
													foreach (Task task in tasksToAdd)
				                        			{
				                        				TaskList.Add(task);
				                        			}

				                        			SaveTasks();
				                        			PushLocal();
				                        		}
				                        	},
				                        LogExceptionAndReadyUp
					);
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
			Trace.Write(PhoneLogger.LogLevel.Debug, "Saving {0}", GetFileName());

			using (IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
			{
				using (IsolatedStorageFileStream file = appStorage.OpenFile(GetFileName(), FileMode.Create, FileAccess.Write))
				{
					TaskList.SaveTasks(file);
				}
			}

			LocalHasChanges = true;
		}

		private void OverwriteWithRemoteFile(RestResponse response, DateTime remoteModifiedTime)
		{
			using (IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
			{
				using (IsolatedStorageFileStream file = appStorage.OpenFile(GetFileName(), FileMode.Create))
				{
					using (var writer = new StreamWriter(file))
					{
						writer.Write(response.Content);
						writer.Flush();
					}
				}
			}

			LocalLastSynced = remoteModifiedTime;
			LocalHasChanges = false;
			LoadTasks();
		}

		private void UseRemoteFile(DateTime remoteModifiedTime)
		{
			Trace.Write(PhoneLogger.LogLevel.Debug,
			            "Using remote version of {0}", GetFileName());

			_dropBoxService.GetFile(FullPath,
			                        response => OverwriteWithRemoteFile(response, remoteModifiedTime),
			                        LogExceptionAndReadyUp);
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
		public PrimaryTaskFileService(DropBoxService dropBoxService, ApplicationSettings settings)
			: base(dropBoxService, settings)
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
		public ArchiveTaskFileService(DropBoxService dropBoxService, ApplicationSettings settings)
			: base(dropBoxService, settings)
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