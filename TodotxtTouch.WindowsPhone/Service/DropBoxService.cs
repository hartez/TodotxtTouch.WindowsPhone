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

		public DropBoxService()
		{
			Messenger.Default.Register<CredentialsUpdatedMessage>(
					this, (message) =>
					{
						if (!WeHaveTokens)
						{
							Connect();
						}
					});
		}

		public void Connect()
		{
			Connect(null);
		}

		public void Connect(Action connectCallback)
		{
			if (NetworkHelper.GetIsNetworkAvailable())
			{
				if (WeHaveTokens)
				{
					_dropNetClient = DropNetExtensions.CreateClient(Token, Secret);

					if (connectCallback != null)
					{
						connectCallback();
					}
				}
				else if (HasLoginCredentials)
				{
					_dropNetClient = DropNetExtensions.CreateClient();
					_dropNetClient.LoginAsync(Username, Password, 
						(response) =>
						{
							Token = response.Token;
							Secret = response.Secret;

							if(connectCallback != null)
							{
								connectCallback();
							}
						},
						(exception) => Trace.Write(PhoneLogger.LogLevel.Error,
						                           exception.Message));
				}
				else
				{
					Messenger.Default.Send(new NeedCredentialsMessage());
				}
			}
		}

		public bool WeHaveTokens
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
			get { return NetworkHelper.GetIsNetworkAvailable() && WeHaveTokens; } 
		}

		public void GetMetaData(string path, Action<MetaData> success, Action<DropboxException> failure)
		{
			if (Accessible)
			{
				try
				{
					_dropNetClient.GetMetaDataAsync(path, success, failure);
				}
				catch (Exception ex)
				{
					// TODO Figure out why done.txt keeps giving us metadata issues
					Trace.Write(PhoneLogger.LogLevel.Error, ex.ToString());
				}
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