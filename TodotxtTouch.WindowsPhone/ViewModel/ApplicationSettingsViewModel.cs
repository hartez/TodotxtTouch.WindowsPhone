﻿using System.Collections.Generic;
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

        public const string SyncOnStartupPropertyName = "SyncOnStartup";

        private const string ConnectedPropertyName = "Connected";
	    private const string DisconnectedPropertyName = "Disconnected";

        public bool Connected => _dropBoxService.WeHaveTokens;

		public bool Disconnected => !Connected;

		private readonly ApplicationSettings _settings;
        private readonly DropboxService _dropBoxService;

		public RelayCommand BroadcastSettingsChanged { get; private set; }

		public ApplicationSettingsViewModel(ApplicationSettings settings, DropboxService dropBoxService)
        {
		    _settings = settings;
		    _dropBoxService = dropBoxService;

		    BroadcastSettingsChanged =
		        new RelayCommand(() => Messenger.Default.Send(new ApplicationSettingsChangedMessage(_settings)));
		    DisconnectCommand = new RelayCommand(Disconnect);
            ResetColorsCommand = new RelayCommand(ResetColors);
        }

        public RelayCommand DisconnectCommand { get; private set; }
        public RelayCommand ResetColorsCommand { get; private set; }

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

	    public void ResetColors()
	    {
	        _settings.ResetColors();
            RaisePropertyChanged("PriorityColors");
	    }

	    /// <summary>
		/// Gets the TodoFileName property.
		/// Changes to that property's value raise the PropertyChanged event. 
		/// </summary>
		public string TodoFileName
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
		public string TodoFilePath
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
		public string ArchiveFilePath
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
		public string ArchiveFileName
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

        /// <summary>
        /// Determines whether the application should attempt to sync to DropBox immediately on startup
        /// </summary>
        public bool SyncOnStartup
        {
            get { return _settings.SyncOnStartup; }

            set
            {
                if (_settings.SyncOnStartup == value)
                {
                    return;
                }

                _settings.SyncOnStartup = value;

                // Update bindings, no broadcast
                RaisePropertyChanged(SyncOnStartupPropertyName);
            }
        }

        public List<PriorityColor> PriorityColors => _settings.PriorityColors;

		public List<ColorOption> PriorityColorOptions => ColorOptions.All;
	}
}