using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Windows.Navigation;
using DropNet.Exceptions;
using DropNet.Models;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Phone.Reactive;
using RestSharp;
using todotxtlib.net;
using TodotxtTouch.WindowsPhone.Messages;
using TodotxtTouch.WindowsPhone.ViewModel;

namespace TodotxtTouch.WindowsPhone.Service
{
	public abstract class TaskFileService
	{
		protected readonly ApplicationSettings Settings;
		private readonly DropboxService _dropBoxService;
		private TaskList _taskList = new TaskList();
		private TaskLoadingState _loadingState = TaskLoadingState.Ready;
		private string _lastRevision;

		private readonly object _syncLock = new object();

		protected TaskFileService(DropboxService dropBoxService, ApplicationSettings settings)
		{
			_dropBoxService = dropBoxService;
			Settings = settings;

			_taskList.CollectionChanged += TaskListCollectionChanged;

			Messenger.Default.Register<ApplicationReadyMessage>(this, message => Start());
			Messenger.Default.Register<NeedCredentialsMessage>(this, message =>
				{
					if(LoadingState == TaskLoadingState.Syncing){LoadingState = TaskLoadingState.Ready;}
				});
		}

		private void Start()
		{
			LoadTasks();
		}

		public bool LocalHasChanges
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
		    private set
		    {
		        IsolatedStorageSettings.ApplicationSettings[GetFileName() + "haschanges"] = value;
		        InvokeLocalHasChangesChanged(new LocalHasChangesChangedEventArgs(value));
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

		private String LocalLastRevisionPropertyName
		{
            get { return GetFileName() + "LocalLastRevision"; }
		}

		private string LocalLastRevision
		{
			get
			{
				if (_lastRevision == null)
				{
					string llm;
					if (IsolatedStorageSettings.ApplicationSettings.TryGetValue(LocalLastRevisionPropertyName, out llm))
					{
						_lastRevision = llm;
					}
				}

				return _lastRevision;
			}
			set
			{
				_lastRevision = value;
				IsolatedStorageSettings.ApplicationSettings[LocalLastRevisionPropertyName] = _lastRevision;
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

		private void PauseCollectionChanged()
		{
			_taskList.CollectionChanged -= TaskListCollectionChanged;
		}

		private void ResumeCollectionChanged()
		{
			_taskList.CollectionChanged += TaskListCollectionChanged;
		}

		private void ClearTaskPropertyChangedHandlers()
		{
			foreach(var task in _taskList)
			{
				task.PropertyChanged -= TaskPropertyChanged;
			}
		}

		private void InitTaskPropertyChangedHandlers()
		{
			foreach (var task in _taskList)
			{
				task.PropertyChanged += TaskPropertyChanged;
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
			if (LoadingState != TaskLoadingState.Syncing)
			{
				LoadingState = TaskLoadingState.Syncing;

				Messenger.Default.Register<NetworkUnavailableMessage>(this,
				                                                      msg =>
				                                                      	{
				                                                      		LoadingState = TaskLoadingState.Ready;
				                                                      		Messenger.Default.Unregister<NetworkUnavailableMessage>(this);
				                                                      	});

				// Get the metadata for the remote file
				GetRemoteMetaData(Sync, exception => Sync(null));
			}
		}

		private void Sync(MetaData data)
		{
			bool remoteExists = (data != null && !String.IsNullOrEmpty(data.Name));

			if (!remoteExists)
			{
				if (LocalFileExists)
				{
					// If there's no remote file but there is a local file,
					// then we need to push the local file up
					PushLocal();
					return;
				}

				// No remote and no local? Then save the current task list (even if empty) as the local file
				SaveTasks();
				LoadTasks();
				return;
			}

            // This should really use the rev property, but that's not in the current version of DropNet
			string remoteRevision = data.Revision.ToString(CultureInfo.InvariantCulture);

			// See if we have a local task file
			if (!LocalFileExists)
			{
				// We have no local file - just make the remote file the local file
				UseRemoteFile(remoteRevision);
				return;
			}

			// Use the metadata to make a decision about whether to 
			// get/merge the remote file
			if (!String.IsNullOrEmpty(LocalLastRevision))
			{
				if (LocalLastRevision == remoteRevision && !LocalHasChanges)
				{
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
				else if (LocalLastRevision != remoteRevision && !LocalHasChanges)
				{
					//	If local.Retrieved < remote.LastUpdated and local has no changes, replace local with remote (local.Retrieved = remote.LastUpdated)
					IsolatedStorageSettings.ApplicationSettings["LastLocalModified"] = remoteRevision;
					UseRemoteFile(remoteRevision);
				}
				else if (LocalLastRevision != remoteRevision && LocalHasChanges)
				{
					//If local.Retrieved < remote.LastUpdated and local has changes, merge (???) or maybe just upload local to conflicted file?
					IntiateMerge();
				}
				else if (LocalLastRevision == remoteRevision && LocalHasChanges)
				{
					//If local.Retrieved == remote.LastUpdate and local has changes, upload local
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
				// The only reason for this to happen would be that a local file was created before any 
				// synchronization was done. So we should merge any local stuff with whatever is in the remote
				IntiateMerge();
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

			var buffer = new byte[initialLength];
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
					var newBuffer = new byte[buffer.Length*2];
					Array.Copy(buffer, newBuffer, buffer.Length);
					newBuffer[read] = (byte) nextByte;
					buffer = newBuffer;
					read++;
				}
			}
			// Buffer is now too big. Shrink it.
			var ret = new byte[read];
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
			string localFile = GetFileName();

			byte[] bytes = ReadLocalFile(localFile);

			_dropBoxService.Upload(GetFilePath(), localFile, bytes,
			                       response => GetRemoteMetaData(metaDataResponse =>
			                       	{
			                       		LocalHasChanges = false;

										LocalLastRevision = metaDataResponse.Revision.ToString(CultureInfo.InvariantCulture);

                                        CacheForMerge();

			                       		LoadingState = TaskLoadingState.Ready;
			                       	}, SendSyncError), SendSyncError);
		}

		private void SendSyncError(Exception ex)
		{
			LoadingState = TaskLoadingState.Ready;
			InvokeSynchronizationError(new SynchronizationErrorEventArgs(ex));
		}

		private void IntiateMerge()
		{
			_dropBoxService.GetFile(FullPath,
			                        response =>
			                        	{
			                        		MergeTaskLists(response.Content);
			                        	}, SendSyncError);
		}

		private void MergeTaskLists(string remoteTaskContents)
		{
            // We need a tasklist from the original remote file
		    var original = new TaskList();
            
		    using(IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
		    {
		        if(appStorage.FileExists("MergeCache"))
		        {
		            using(IsolatedStorageFileStream file = appStorage.OpenFile("MergeCache", FileMode.Open, FileAccess.Read))
		            {
		                original.LoadTasks(file);
		            }
		        }
		    }

		    // Now we need the new remote task list
            var tl = new TaskList();

			using (var ms = new MemoryStream(
				Encoding.UTF8.GetBytes(remoteTaskContents)))
			{
				tl.LoadTasks(ms);
			}

            // Now that we have the original and updated remote versions, we can merge them
            // with the local version
            LoadingState = TaskLoadingState.Loading;

		    lock(_syncLock)
		    {
		        PauseCollectionChanged();
		        ClearTaskPropertyChangedHandlers();

		        var newTaskList = TaskList.Merge(original, tl, _taskList);

                _taskList.Clear();
		        foreach(var task in newTaskList)
		        {
		            _taskList.Add(task);
		        }

                SaveToStorage();
                PushLocal();

		        InitTaskPropertyChangedHandlers();
		        ResumeCollectionChanged();
		    }

            InvokeTaskListChanged(new TaskListChangedEventArgs());

            LoadingState = TaskLoadingState.Ready;
		}

		private void LoadTasks()
		{
			LoadingState = TaskLoadingState.Loading;

		    if(LocalFileExists)
		    {
		        lock(_syncLock)
		        {
		            PauseCollectionChanged();
		            ClearTaskPropertyChangedHandlers();

		            using(IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
		            {
		                using(
		                    IsolatedStorageFileStream file = appStorage.OpenFile(GetFileName(), FileMode.Open, FileAccess.Read)
		                    )
		                {
		                    TaskList.LoadTasks(file);
		                }
		            }

		            InitTaskPropertyChangedHandlers();
		            ResumeCollectionChanged();
		        }

		        InvokeTaskListChanged(new TaskListChangedEventArgs());
		    }

		    LoadingState = TaskLoadingState.Ready;
		}

	    private void SaveToStorage()
	    {
            lock (_syncLock)
            {
                using (IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    using (IsolatedStorageFileStream file = appStorage.OpenFile(GetFileName(), FileMode.Create, FileAccess.Write))
                    {
                        TaskList.SaveTasks(file);
                    }
                }
            }

            LocalHasChanges = true;
	    }

	    public void SaveTasks()
		{
			TaskLoadingState prevState = LoadingState;

			LoadingState = TaskLoadingState.Saving;

			SaveToStorage();

            InvokeTaskListChanged(new TaskListChangedEventArgs());

			LoadingState = prevState;
		}

	    private void CacheForMerge()
	    {
	        using(IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
	        {
                CacheForMerge(appStorage);
	        }
	    }

	    private void CacheForMerge(IsolatedStorageFile appStorage)
	    {
            if (appStorage.FileExists("MergeCache"))
            {
                appStorage.DeleteFile("MergeCache");
            }

            appStorage.CopyFile(GetFileName(), "MergeCache");
	    }

	    private void OverwriteWithRemoteFile(IRestResponse response, string latestRevision)
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

			    CacheForMerge(appStorage);
			}

			LocalLastRevision = latestRevision;
			LocalHasChanges = false;
			LoadTasks();
		}

		private void UseRemoteFile(String latestRevision)
		{
			_dropBoxService.GetFile(FullPath,
			                        response => OverwriteWithRemoteFile(response, latestRevision),
			                        ex => InvokeSynchronizationError(new SynchronizationErrorEventArgs(ex)));
		}

		#region Events

        public event EventHandler<LocalHasChangesChangedEventArgs> LocalHasChangesChanged;
		public event EventHandler<LoadingStateChangedEventArgs> LoadingStateChanged;
		public event EventHandler<TaskListChangedEventArgs> TaskListChanged;
		public event EventHandler<SynchronizationErrorEventArgs> SynchronizationError;

		public void InvokeSynchronizationError(SynchronizationErrorEventArgs e)
		{
			EventHandler<SynchronizationErrorEventArgs> handler = SynchronizationError;
			if (handler != null)
			{
				handler(this, e);
			}
		}

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


        public void InvokeLocalHasChangesChanged(LocalHasChangesChangedEventArgs e)
        {
            EventHandler<LocalHasChangesChangedEventArgs> handler = LocalHasChangesChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

		#endregion
	}
}