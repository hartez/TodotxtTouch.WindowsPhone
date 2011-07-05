using System;
using System.IO.IsolatedStorage;
using AgiliTrain.PhoneyTools;
using DropNet;
using DropNet.Exceptions;
using DropNet.Models;
using GalaSoft.MvvmLight.Messaging;
using RestSharp;
using TodotxtTouch.WindowsPhone.ViewModel;
using NetworkInterface = System.Net.NetworkInformation.NetworkInterface;

namespace TodotxtTouch.WindowsPhone.Service
{
	public class DropBoxService
	{
		private String _username = String.Empty;
		private DropNetClient _dropNetClient;

		private String _password = String.Empty;
		private String _secret = String.Empty;
		private String _token = String.Empty;

		public event EventHandler<DropBoxServiceAvailableChangedEventArgs> DropBoxServiceConnectedChanged;

		public void InvokeDropBoxServiceConnectedChanged(DropBoxServiceAvailableChangedEventArgs e)
		{
			EventHandler<DropBoxServiceAvailableChangedEventArgs> handler = DropBoxServiceConnectedChanged;
			if (handler != null)
			{
				handler(this, e);
			}
		}

		private bool _authenticated = false;

		public bool Authenticated
		{
			get
			{
				return _authenticated;
			}
			set
			{
				_authenticated = value;
				InvokeDropBoxServiceConnectedChanged(new DropBoxServiceAvailableChangedEventArgs());
			}
		}

		public DropBoxService()
		{
			Messenger.Default.Register<ApplicationReadyMessage>(
				this, (message) =>
					{
						if(!Authenticated)
						{
							Connect();
						}
					});

			Messenger.Default.Register<CredentialsUpdatedMessage>(
					this, (message) =>
					{
						if (!Authenticated)
						{
							Connect();
						}
					});

			Connect();
		}

		private void Connect()
		{
			if (NetworkInterface.GetIsNetworkAvailable())
			{
				if (IsAuthenticated)
				{
					_dropNetClient = DropNetExtensions.CreateClient(Token, Secret);
					Authenticated = true;
				}
				else if (HasLoginCredentials)
				{
					_dropNetClient = DropNetExtensions.CreateClient();
					_dropNetClient.LoginAsync(Username, Password, 
						(response) =>
						{
							Token = response.Token;
							Secret = response.Secret;
							Authenticated = true;
						},
						(exception) =>
							{
								Trace.Write(PhoneLogger.LogLevel.Error,
								            exception.Message);
								Authenticated = false;
							}
						);
				}
				else
				{
					Messenger.Default.Send(new NeedCredentialsMessage());
				}
			}
		}

		public bool IsAuthenticated
		{
			get { return !String.IsNullOrEmpty(Token) && !String.IsNullOrEmpty(Secret); }
		}

		public bool HasLoginCredentials
		{
			get { return !String.IsNullOrEmpty(Password) && !String.IsNullOrEmpty(Username); }
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
				IsolatedStorageSettings.ApplicationSettings["dropboxSecret"] = _secret;
			}
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
				IsolatedStorageSettings.ApplicationSettings["dropboxToken"] = _token;
			}
		}

		/// <summary>
		/// Gets the Username property.
		/// Changes to that property's value raise the PropertyChanged event. 
		/// </summary>
		public string Username
		{
			get
			{
				if (String.IsNullOrEmpty(_username))
				{
					String username;
					if (IsolatedStorageSettings.ApplicationSettings.TryGetValue("dropboxUsername",
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
				IsolatedStorageSettings.ApplicationSettings["dropboxUsername"] = _username;
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
			}
		}

		public bool Accessible
		{
			get { return NetworkInterface.GetIsNetworkAvailable() && Authenticated; } 
		}

		public void GetMetaData(string path, Action<MetaData> success, Action<DropboxException> failure)
		{
			if (Accessible)
			{
				_dropNetClient.GetMetaDataAsync(path, success, failure);
			}
		}

		public void Upload(string path, string filename, byte[] bytes, Action<RestResponse> success, Action<DropboxException> failure)
		{
			if (Accessible)
			{
				_dropNetClient.UploadFileAsync(path, filename, bytes, success, failure);
			}
		}

		public void GetFile(string path, Action<RestResponse> success, Action<DropboxException> failure)
		{
			if (Accessible)
			{
				_dropNetClient.GetFileAsync(path, success, failure);
			}
		}
	}
}