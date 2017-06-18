﻿using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using TodotxtTouch.WindowsPhone.Messages;
using TodotxtTouch.WindowsPhone.Service;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
	public class DropboxCredentialsViewModel : ViewModelBase
	{
		private readonly DropboxService _dropBoxService;

		/// <summary>
		/// The <see>
		///         <cref>Username</cref>
		///     </see>
		///     property's name.
		/// </summary>
		public const string UsernamePropertyName = "Username";

		/// <summary>
		/// The <see>
		///         <cref>Password</cref>
		///     </see>
		///     property's name.
		/// </summary>
		public const string PasswordPropertyName = "Password";

		public RelayCommand StartLoginProcessCommand { get; private set; }

		/// <summary>
		/// Initializes a new instance of the ApplicationSettingsViewModel class.
		/// </summary>
		public DropboxCredentialsViewModel(DropboxService dropBoxService)
		{
			if (IsInDesignMode)
			{
				// Code runs in Blend --> create design time data.
			}
			else
			{
				// Code runs "for real"
				_dropBoxService = dropBoxService;
				StartLoginProcessCommand = new RelayCommand(StartLoginProcess);
				Messenger.Default.Register<DropboxLoginSuccessfulMessage>(this, msg => _dropBoxService.GetAccessToken(msg));
			}
		}

		private void StartLoginProcess()
		{
			_dropBoxService.StartLoginProcess();
		}
	}
}