using System;
using System.IO.IsolatedStorage;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
	public class DropBoxCredentialsViewModel : ViewModelBase
	{
		/// <summary>
		/// The <see cref="Username" /> property's name.
		/// </summary>
		public const string UsernamePropertyName = "Username";

		/// <summary>
		/// The <see cref="Password" /> property's name.
		/// </summary>
		public const string PasswordPropertyName = "Password";

		/// <summary>
		/// The <see cref="Token" /> property's name.
		/// </summary>
		public const string TokenPropertyName = "Token";

		/// <summary>
		/// The <see cref="Secret" /> property's name.
		/// </summary>
		public const string SecretPropertyName = "Secret";

		private String _password = String.Empty;
		private string _username = String.Empty;
		private string _token;
		private string _secret;

		public RelayCommand UpdateCredentialsCommand { get; private set; }

		private void PersistCredentials()
		{
			// Save credentials 
			IsolatedStorageSettings.ApplicationSettings["dropboxUsername"] = Username;
			IsolatedStorageSettings.ApplicationSettings["dropboxToken"] = Token;
			IsolatedStorageSettings.ApplicationSettings["dropboxSecret"] = Secret;
		}

		/// <summary>
		/// Initializes a new instance of the ApplicationSettingsViewModel class.
		/// </summary>
		public DropBoxCredentialsViewModel()
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
				Password = "23yoink42dropbo";
#endif

				UpdateCredentialsCommand = new RelayCommand(UpdateCredentials);
			}
		}

		private void UpdateCredentials()
		{
			PersistCredentials();

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
				if(String.IsNullOrEmpty(_username))
				{
					String username;
					if(IsolatedStorageSettings.ApplicationSettings.TryGetValue("dropboxUsername",
					                                                        out username))
					{
						_username = username;
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

		public bool IsAuthenticated
		{
			get { return !String.IsNullOrEmpty(Token) && !String.IsNullOrEmpty(Secret); }
		}

		public bool HasLoginCredentials
		{
			get { return !String.IsNullOrEmpty(Password) && !String.IsNullOrEmpty(Username) && Password != "23yoink42dropbo"; }
		}

		/// <summary>
		/// Gets the Token property.
		/// Changes to that property's value raise the PropertyChanged event. 
		/// </summary>
		public string Token
		{
			get
			{
				if (String.IsNullOrEmpty(_token))
				{
					String token;
					if (IsolatedStorageSettings.ApplicationSettings.TryGetValue("dropboxToken",
																			out token))
					{
						_token = token;
					}
					else
					{
						Messenger.Default.Send(new NeedCredentialsMessage());
					}
				}

				return _token;
			}

			set
			{
				if (_token == value)
				{
					return;
				}

				_token = value;

				// Update bindings, no broadcast
				RaisePropertyChanged(TokenPropertyName);
			}
		}

		/// <summary>
		/// Gets the Secret property.
		/// Changes to that property's value raise the PropertyChanged event. 
		/// </summary>
		public string Secret
		{
			get
			{
				if (String.IsNullOrEmpty(_secret))
				{
					String secret;
					if (IsolatedStorageSettings.ApplicationSettings.TryGetValue("dropboxSecret",
																			out secret))
					{
						_secret = secret;
					}
					else
					{
						Messenger.Default.Send(new NeedCredentialsMessage());
					}
				}

				return _secret;
			}

			set
			{
				if (_secret == value)
				{
					return;
				}

				_secret = value;

				// Update bindings, no broadcast
				RaisePropertyChanged(SecretPropertyName);
			}
		}
	}
}