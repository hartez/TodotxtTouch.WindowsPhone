using System;
using System.IO.IsolatedStorage;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using TodotxtTouch.WindowsPhone.Service;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
	public class DropBoxCredentialsViewModel : ViewModelBase
	{
		private DropBoxService _dropBoxService;

		/// <summary>
		/// The <see cref="Username" /> property's name.
		/// </summary>
		public const string UsernamePropertyName = "Username";

		/// <summary>
		/// The <see cref="Password" /> property's name.
		/// </summary>
		public const string PasswordPropertyName = "Password";

		public RelayCommand UpdateCredentialsCommand { get; private set; }

		/// <summary>
		/// Initializes a new instance of the ApplicationSettingsViewModel class.
		/// </summary>
		public DropBoxCredentialsViewModel(DropBoxService dropBoxService)
		{
			if (IsInDesignMode)
			{
				// Code runs in Blend --> create design time data.
			}
			else
			{
				// Code runs "for real"
				_dropBoxService = dropBoxService;

#if DEBUG
				Username = "hartez@gmail.com";
				Password = "23yoink42dropbo";
#endif

				UpdateCredentialsCommand = new RelayCommand(UpdateCredentials);
			}
		}

		private void UpdateCredentials()
		{
			Messenger.Default.Send(new CredentialsUpdatedMessage());
		}

		/// <summary>
		/// Gets the Username property.
		/// Changes to that property's value raise the PropertyChanged event. 
		/// </summary>
		public string Username
		{
			get
			{
				return _dropBoxService.Username;
			}

			set
			{
				_dropBoxService.Username = value;

				// Update bindings, no broadcast
				RaisePropertyChanged(UsernamePropertyName);
			}
		}

		/// <summary>
		/// Gets the Password property.
		/// Changes to that property's value raise the PropertyChanged event. 
		/// </summary>
		public String Password
		{
			get
			{
				return _dropBoxService.Password;
			}

			set
			{
				_dropBoxService.Password = value;

				// Update bindings, no broadcast
				RaisePropertyChanged(PasswordPropertyName);
			}
		}
	}
}