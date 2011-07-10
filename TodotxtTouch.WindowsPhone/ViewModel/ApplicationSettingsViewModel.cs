using System;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using TodotxtTouch.WindowsPhone.Messages;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
	public class ApplicationSettingsViewModel : ViewModelBase
	{
		/// <summary>
		/// The <see cref="TodoFileName" /> property's name.
		/// </summary>
		public const string TodoFileNamePropertyName = "TodoFileName";

		/// <summary>
		/// The <see cref="ArchiveFileName" /> property's name.
		/// </summary>
		public const string ArchiveFileNamePropertyName = "ArchiveFileName";

		/// <summary>
		/// The <see cref="ArchiveFilePath" /> property's name.
		/// </summary>
		public const string ArchiveFilePathPropertyName = "ArchiveFilePath";

		/// <summary>
		/// The <see cref="TodoFilePath" /> property's name.
		/// </summary>
		public const string TodoFilePathPropertyName = "TodoFilePath";

		private readonly ApplicationSettings _settings;

		public RelayCommand BroadcastSettingsChanged { get; private set; }

		public ApplicationSettingsViewModel(ApplicationSettings settings)
		{
			_settings = settings;

			BroadcastSettingsChanged = new RelayCommand(() => Messenger.Default.Send(new ApplicationSettingsChangedMessage(_settings)));

			if (IsInDesignMode)
			{
			}
			else
			{
			}
		}

		/// <summary>
		/// Gets the TodoFileName property.
		/// Changes to that property's value raise the PropertyChanged event. 
		/// </summary>
		public String TodoFileName
		{
			get { return _settings.TodoFileName; }

			set
			{
				if (_settings.TodoFileName == value)
				{
					return;
				}

				_settings.TodoFileName = value;

				// Update bindings, no broadcast
				RaisePropertyChanged(TodoFileNamePropertyName);
			}
		}

		/// <summary>
		/// Gets the TodoFilePath property.
		/// Changes to that property's value raise the PropertyChanged event. 
		/// </summary>
		public String TodoFilePath
		{
			get { return _settings.TodoFilePath; }

			set
			{
				if (_settings.TodoFilePath == value)
				{
					return;
				}

				_settings.TodoFilePath = value;

				// Update bindings, no broadcast
				RaisePropertyChanged(TodoFilePathPropertyName);
			}
		}

		/// <summary>
		/// Gets the ArchiveFilePath property.
		/// Changes to that property's value raise the PropertyChanged event. 
		/// </summary>
		public String ArchiveFilePath
		{
			get { return _settings.ArchiveFilePath; }

			set
			{
				if (_settings.ArchiveFilePath == value)
				{
					return;
				}

				_settings.ArchiveFilePath = value;

				// Update bindings, no broadcast
				RaisePropertyChanged(ArchiveFilePathPropertyName);
			}
		}

		/// <summary>
		/// Gets the ArchiveFileName property.
		/// Changes to that property's value raise the PropertyChanged event. 
		/// </summary>
		public String ArchiveFileName
		{
			get { return _settings.ArchiveFileName; }

			set
			{
				if (_settings.ArchiveFileName == value)
				{
					return;
				}

				_settings.ArchiveFileName = value;

				// Update bindings, no broadcast
				RaisePropertyChanged(ArchiveFileNamePropertyName);
			}
		}
	}
}