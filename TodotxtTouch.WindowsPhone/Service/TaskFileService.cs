using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text;
using System.Threading.Tasks;
using Dropbox.Api.Files;
using GalaSoft.MvvmLight.Messaging;
using todotxtlib.net;
using TodotxtTouch.WindowsPhone.Messages;
using TodotxtTouch.WindowsPhone.ViewModel;
using Task = System.Threading.Tasks.Task;
using TTask = todotxtlib.net.Task;

namespace TodotxtTouch.WindowsPhone.Service
{
	public abstract class TaskFileService
	{
		protected readonly ApplicationSettings Settings;
		private readonly DropboxService _dropBoxService;
		private readonly TaskList _taskList = new TaskList();
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
				IsolatedStorageSettings.ApplicationSettings.Save();
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
		public TaskList TaskList => _taskList;

		private string LocalLastRevisionPropertyName => GetFileName() + "LocalLastRevision";

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
				IsolatedStorageSettings.ApplicationSettings.Save();
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

		private string FullPath => GetFilePath() + "/" + GetFileName();

		protected abstract string GetFilePath();
		protected abstract string GetFileName();

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
				foreach (TTask item in e.OldItems)
				{
					//Removed items
					item.PropertyChanged -= TaskPropertyChanged;
				}
			}
			else if (e.Action == NotifyCollectionChangedAction.Add)
			{
				foreach (TTask item in e.NewItems)
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

		public void UpdateTask(TTask task, TTask oldTask)
		{
			int index = TaskList.IndexOf(oldTask);
			TaskList[index].UpdateTo(task);
		}

		private async Task<Metadata> GetRemoteMetaData()
		{
			return await _dropBoxService.GetMetaDataAsync(FullPath);
		}

		public async Task Sync()
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
				var metadata = await GetRemoteMetaData();

				if (metadata == null)
				{
					await InitRemote();
				}
				else
				{
					await Sync(metadata);
				}
			}
		}

		private async Task InitRemote()
		{
			if (LocalFileExists)
			{
				// If there's no remote file but there is a local file,
				// then we need to push the local file up
				await PushLocal();
				return;
			}

			// No remote and no local? Then save the current task list (even if empty) as the local file
			SaveTasks();
			LoadTasks();
			return;
		}

		private async Task Sync(Metadata data)
		{
			string remoteRevision = data.AsFile.Rev.ToString(CultureInfo.InvariantCulture);

			// See if we have a local task file
			if (!LocalFileExists)
			{
				// We have no local file - just make the remote file the local file
				await UseRemoteFile();
			}

			// Use the metadata to make a decision about whether to 
			// get/merge the remote file
			if (!string.IsNullOrEmpty(LocalLastRevision))
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
					await UseRemoteFile();
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

					await PushLocal();
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

		private async Task PushLocal(string remoteRevision = null)
		{
			string localFile = GetFileName();

			byte[] bytes = ReadLocalFile(localFile);

			try
			{
				var metadata = await _dropBoxService.UploadAsync(GetFilePath(), localFile, remoteRevision ?? LocalLastRevision, bytes);
				LocalHasChanges = false;
				LocalLastRevision = metadata.AsFile.Rev.ToString(CultureInfo.InvariantCulture);
				CacheForMerge();
				LoadingState = TaskLoadingState.Ready;
			}
			catch (Exception ex)
			{
				SendSyncError(ex);
			}
		}

		private void SendSyncError(Exception ex)
		{
			LoadingState = TaskLoadingState.Ready;
			InvokeSynchronizationError(new SynchronizationErrorEventArgs(ex));
		}

		private async void IntiateMerge()
		{
			try
			{
				var response = await _dropBoxService.GetFileAsync(FullPath);
				var remoteContents = await response.GetContentAsStringAsync();
				await MergeTaskLists(remoteContents, response.Response.Rev);
			}
			catch (Exception ex)
			{
				SendSyncError(ex);
			}
		}

		private async Task MergeTaskLists(string remoteTaskContents, string remoteRevision)
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

			// TODO hartez 2017/06/04 15:37:32 Think through whether this lock statement makes any sense	
		    //lock(_syncLock)
		    //{
		        PauseCollectionChanged();
		        ClearTaskPropertyChangedHandlers();

		        var newTaskList = TaskList.Merge(original, tl, _taskList);

                _taskList.Clear();
		        foreach(var task in newTaskList)
		        {
		            _taskList.Add(task);
		        }

                SaveToStorage();
				await PushLocal(remoteRevision);

		        InitTaskPropertyChangedHandlers();
		        ResumeCollectionChanged();
		    //}

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

	    private void OverwriteWithRemoteFile(string remoteContent, string revision)
		{
			using (IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
			{
				using (IsolatedStorageFileStream file = appStorage.OpenFile(GetFileName(), FileMode.Create))
				{
					using (var writer = new StreamWriter(file))
					{
						writer.Write(remoteContent);
						writer.Flush();
					}
				}

			    CacheForMerge(appStorage);
			}

			LocalLastRevision = revision;
			LocalHasChanges = false;
			LoadTasks();
		}

		private async Task UseRemoteFile()
		{
			try
			{
				var response = await _dropBoxService.GetFileAsync(FullPath);
				var contents = await response.GetContentAsStringAsync();

				OverwriteWithRemoteFile(contents, response.Response.Rev);
			}
			catch (Exception ex)
			{
				InvokeSynchronizationError(new SynchronizationErrorEventArgs(ex));
			}
		}

		#region Events

        public event EventHandler<LocalHasChangesChangedEventArgs> LocalHasChangesChanged;
		public event EventHandler<LoadingStateChangedEventArgs> LoadingStateChanged;
		public event EventHandler<TaskListChangedEventArgs> TaskListChanged;
		public event EventHandler<SynchronizationErrorEventArgs> SynchronizationError;

		public void InvokeSynchronizationError(SynchronizationErrorEventArgs e)
		{
			var handler = SynchronizationError;
			handler?.Invoke(this, e);
		}

		public void InvokeTaskListChanged(TaskListChangedEventArgs e)
		{
			var handler = TaskListChanged;
			handler?.Invoke(this, e);
		}

		public void InvokeLoadingStateChanged(LoadingStateChangedEventArgs e)
		{
			var handler = LoadingStateChanged;
			handler?.Invoke(this, e);
		}

        public void InvokeLocalHasChangesChanged(LocalHasChangesChangedEventArgs e)
        {
            var handler = LocalHasChangesChanged;
	        handler?.Invoke(this, e);
        }

		#endregion
	}
}