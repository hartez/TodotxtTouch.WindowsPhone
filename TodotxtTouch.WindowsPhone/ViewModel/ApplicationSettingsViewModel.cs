using System;
using System.IO.IsolatedStorage;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
	public class ApplicationSettingsViewModel : ViewModelBase
	{
		/// <summary>
		/// The <see cref="Username" /> property's name.
		/// </summary>
		public const string UsernamePropertyName = "Username";

		/// <summary>
		/// The <see cref="Password" /> property's name.
		/// </summary>
		public const string PasswordPropertyName = "Password";

		private String _password = String.Empty;
		private string _username = String.Empty;

		public RelayCommand UpdateSettingsCommand { get; private set; }

		private void PersistSettings()
		{
			// Check application settings for whether to store credentials

			// Save credentials if we're supposed to
			IsolatedStorageSettings.ApplicationSettings["dropboxUsername"] = Username;
			IsolatedStorageSettings.ApplicationSettings["dropboxPassword"] = Password;
		}

		/// <summary>
		/// Initializes a new instance of the ApplicationSettingsViewModel class.
		/// </summary>
		public ApplicationSettingsViewModel()
		{
			if (IsInDesignMode)
			{
				// Code runs in Blend --> create design time data.
			}
			else
			{
				// Code runs "for real"
#if DEBUG
				Username = "hartez@gmail.com";
				Password = "23yoink42dropbox";
#endif

				UpdateSettingsCommand = new RelayCommand(UpdateSettings);
			}
		}

		private void UpdateSettings()
		{
			PersistSettings();

			Messenger.Default.Send(new SettingsUpdatedMessage());
		}

		/// <summary>
		/// Gets the Username property.
		/// Changes to that property's value raise the PropertyChanged event. 
		/// </summary>
		public string Username
		{
			get
			{
				if(String.IsNullOrEmpty(_username))
				{
					String username;
					if(IsolatedStorageSettings.ApplicationSettings.TryGetValue("dropboxUsername",
					                                                        out username))
					{
						_username = username;
					}
					else
					{
						Messenger.Default.Send(new NeedCredentialsMessage());
					}
				}

				return _username;
			}

			set
			{
				if (_username == value)
				{
					return;
				}

				_username = value;

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
				if (String.IsNullOrEmpty(_password))
				{
					String password;
					if (IsolatedStorageSettings.ApplicationSettings.TryGetValue("dropboxPassword", out password))
					{
						_password = password;
					}
					else
					{
						Messenger.Default.Send(new NeedCredentialsMessage());
					}
				}

				return _password;
			}

			set
			{
				if (_password == value)
				{
					return;
				}

				_password = value;

				// Update bindings, no broadcast
				RaisePropertyChanged(PasswordPropertyName);
			}
		}

		public bool HasCredentials
		{
			get { return !String.IsNullOrEmpty(Username) && !String.IsNullOrEmpty(Password); }
		}
	}
}