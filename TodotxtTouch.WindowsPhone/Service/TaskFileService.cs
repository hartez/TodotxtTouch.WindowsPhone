using System;
using System.IO;
using System.IO.IsolatedStorage;
using DropNet;
using DropNet.Models;
using GalaSoft.MvvmLight.Messaging;
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

		public TaskFileService(DropBoxCredentialsViewModel dropBoxCredentialsViewModel, string taskFileName)
		{
			_dropBoxCredentials = dropBoxCredentialsViewModel;
			this.taskFileName = taskFileName;

			Messenger.Default.Register<CredentialsUpdatedMessage>(
				this, message => Sync());

			Messenger.Default.Register<ApplicationReadyMessage>(
				this, (message) =>
				{
					if (HaveLocalFile)
					{
						LoadTasks();
					}

					Sync();
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

		public event EventHandler<LoadingStateChangedEventArgs> LoadingStateChanged;

		public void InvokeLoadingStateChanged(LoadingStateChangedEventArgs e)
		{
			EventHandler<LoadingStateChangedEventArgs> handler = LoadingStateChanged;
			if (handler != null)
			{
				handler(this, e);
			}
		}

		public void AddTask(Task task)
		{
			LoadingState = TaskLoadingState.Syncing;

			TaskList.Add(task);
			_localHasChanges = true;

			SaveTasks();
			Sync();
		}

		public void UpdateTask(Task task, Task oldTask)
		{
			LoadingState = TaskLoadingState.Syncing;

			int index = TaskList.IndexOf(oldTask);
			TaskList[index] = task;

			_localHasChanges = true;

			SaveTasks();
			Sync();
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
					TaskList.LoadTasks(file);
					LoadingState = TaskLoadingState.Ready;
				}
			}
		}

		private void SaveTasks()
		{
			using (IsolatedStorageFile appStorage = IsolatedStorageFile.GetUserStoreForApplication())
			{
				using (IsolatedStorageFileStream file = appStorage.OpenFile(taskFileName, FileMode.Open, FileAccess.Write))
				{
					TaskList.SaveTasks(file);
				}
			}
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