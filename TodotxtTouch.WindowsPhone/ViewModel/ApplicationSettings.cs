using System;
using System.IO.IsolatedStorage;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using TodotxtTouch.WindowsPhone.Service;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
	public class ApplicationSettings : ViewModelBase
	{
		private string _archiveFileName;
		private string _archiveFilePath;
		private string _todoFileName;
		private string _todoFilePath;

	    public string ArchiveFilePath
		{
			get
			{
				if (String.IsNullOrEmpty(_archiveFilePath))
				{
                    _archiveFilePath = GetSetting("archiveFilePath", "/todo");
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
                    _archiveFileName = GetSetting("archiveFileName", "done.txt");
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
                    _todoFilePath = GetSetting("todoFilePath", "/todo");
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
                    _todoFileName = GetSetting("todoFileName", "todo.txt");
				}

				return _todoFileName;
			}
			set
			{
				_todoFileName = value;
				IsolatedStorageSettings.ApplicationSettings["todoFileName"] = _todoFileName;
			}
		}

		private static T GetSetting<T>(string setting, T defaultValue)
		{
			T value;
			return IsolatedStorageSettings.ApplicationSettings.TryGetValue(setting,
			    out value) ? value : defaultValue;
		}
	}
}