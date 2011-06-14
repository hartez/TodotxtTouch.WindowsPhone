using System;
using System.IO.IsolatedStorage;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
	public class ApplicationSettings
	{
		private string _archiveFileName;
		private string _archiveFilePath;
		private string _todoFileName;
		private string _todoFilePath;

		public ApplicationSettings()
		{
			_todoFileName = "todo.txt";
			_archiveFileName = "done.txt";
#if DEBUG	
			_todoFileName = "testingtodo.txt";
			_archiveFileName = "testingdone.txt";
#endif
			_todoFilePath = "/todo";
			_archiveFilePath = "/todo";
			
		}

		public string ArchiveFilePath
		{
			get
			{
				if (String.IsNullOrEmpty(_archiveFilePath))
				{
					TryToGetSetting("archiveFilePath", ref _archiveFilePath);
				}

				return _archiveFilePath;
			}
			set
			{
				_archiveFilePath = value;
				IsolatedStorageSettings.ApplicationSettings["archiveFilePath"] = _archiveFilePath;
			}
		}

		public string ArchiveFileName
		{
			get
			{
				if (String.IsNullOrEmpty(_archiveFileName))
				{
					TryToGetSetting("archiveFileName", ref _archiveFileName);
				}

				return _archiveFileName;
			}
			set
			{
				_archiveFileName = value;
				IsolatedStorageSettings.ApplicationSettings["archiveFileName"] = _archiveFileName;
			}
		}


		public String TodoFilePath
		{
			get
			{
				if (String.IsNullOrEmpty(_todoFilePath))
				{
					TryToGetSetting("todoFilePath", ref _todoFilePath);
				}

				return _todoFilePath;
			}
			set
			{
				_todoFilePath = value;
				IsolatedStorageSettings.ApplicationSettings["todoFilePath"] = _todoFilePath;
			}
		}

		public String TodoFileName
		{
			get
			{
				if (String.IsNullOrEmpty(_todoFileName))
				{
					TryToGetSetting("todoFileName", ref _todoFileName);
				}

				return _todoFileName;
			}
			set
			{
				_todoFileName = value;
				IsolatedStorageSettings.ApplicationSettings["todoFileName"] = _todoFileName;
			}
		}

		private static void TryToGetSetting<T>(string setting, ref T current)
		{
			T value;
			if (IsolatedStorageSettings.ApplicationSettings.TryGetValue(setting,
			                                                            out value))
			{
				current = value;
			}
		}
	}
}