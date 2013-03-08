using System;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using TodotxtTouch.WindowsPhone.Messages;
using TodotxtTouch.WindowsPhone.Service;

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

        private const string ConnectedPropertyName = "Connected";
	    private const string DisconnectedPropertyName = "Disconnected";

        public bool Connected
        {
            get
            {
                return _dropBoxService.WeHaveTokens;
            }
        }

	    public bool Disconnected
	    {
            get { return !Connected; }
	    }

	    private readonly ApplicationSettings _settings;
        private DropboxService _dropBoxService;

		public RelayCommand BroadcastSettingsChanged { get; private set; }

		public ApplicationSettingsViewModel(ApplicationSettings settings, DropboxService dropBoxService)
		{
			_settings = settings;
		    _dropBoxService = dropBoxService;

		    BroadcastSettingsChanged = new RelayCommand(() => Messenger.Default.Send(new ApplicationSettingsChangedMessage(_settings)));
            DisconnectCommand = new RelayCommand(Disconnect);

			if (IsInDesignMode)
			{
			}
			else
			{
			}
		}

        public RelayCommand DisconnectCommand { get; private set; }

	    public void CheckConnection()
	    {
            RaisePropertyChanged(ConnectedPropertyName);
            RaisePropertyChanged(DisconnectedPropertyName);
	    }

	    private void Disconnect()
        {
            _dropBoxService.Disconnect();
            RaisePropertyChanged(ConnectedPropertyName);
            RaisePropertyChanged(DisconnectedPropertyName);
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