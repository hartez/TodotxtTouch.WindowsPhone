using System;
using System.IO.IsolatedStorage;
using System.Net;
using DropNet;
using DropNet.Exceptions;
using DropNet.Models;
using GalaSoft.MvvmLight.Messaging;
using RestSharp;
using TodotxtTouch.WindowsPhone.Messages;

namespace TodotxtTouch.WindowsPhone.Service
{
	public class DropboxService
	{
		private DropNetClient _dropNetClient;

		private String _password = String.Empty;
		private String _secret = String.Empty;
		private String _token = String.Empty;
		private String _username = String.Empty;

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
			get { return _password; }

			set
			{
				if (_password == value)
				{
					return;
				}

				_password = value;
			}
		}

		private void ExecuteDropboxAction(Action dropboxAction)
		{
			if (WeHaveTokens)
			{
				if (_dropNetClient == null)
				{
					_dropNetClient = DropNetExtensions.CreateClient(Token, Secret);
				}

				if (dropboxAction != null)
				{
					dropboxAction();
				}
			}
			else if (HasLoginCredentials)
			{
				if (_dropNetClient == null)
				{
					_dropNetClient = DropNetExtensions.CreateClient();
				}

				//_dropNetClient.LoginAsync(Username, Password,
				//                          (response) =>
				//                            {
				//                                Token = response.Token;
				//                                Secret = response.Secret;

				//                                if (dropboxAction != null)
				//                                {
				//                                    dropboxAction();
				//                                }
				//                            },
				//                          WrapExceptionHandler(null));
			}
			else
			{
				Messenger.Default.Register<CredentialsUpdatedMessage>(
					this, (message) =>
						{
							Messenger.Default.Unregister<CredentialsUpdatedMessage>(this);
							ExecuteDropboxAction(dropboxAction);
						});

				Messenger.Default.Send(new NeedCredentialsMessage("Not authenticated"));
			}
		}

		private Action<DropboxException> WrapExceptionHandler(Action<DropboxException> handler)
		{
			return (ex) =>
				{
					// Dropnet responds with BadGateway if the network isn't accessible
					switch (ex.Response.StatusCode)
					{
						case HttpStatusCode.BadGateway:
							Messenger.Default.Send(new NetworkUnavailableMessage());
							break;
						case HttpStatusCode.ServiceUnavailable:
							Messenger.Default.Send(new CannotAccessDropboxMessage("Too many requests"));
							break;
						case HttpStatusCode.InternalServerError:
							Messenger.Default.Send(new CannotAccessDropboxMessage("Dropbox Server Error"));
							break;
						case HttpStatusCode.Unauthorized:
							_dropNetClient = null;
							Token = string.Empty;
							Secret = string.Empty;
							Password = string.Empty;
							Messenger.Default.Send(new NeedCredentialsMessage("Authentication failed"));
							break;
						case HttpStatusCode.BadRequest:
							Messenger.Default.Send(new CannotAccessDropboxMessage("Lacking mobile authentication permission"));
							break;
						default:
							Messenger.Default.Send(new CannotAccessDropboxMessage());
							break;
					}

					if (handler != null)
					{
						handler(ex);
					}
				};
		}

		public void GetMetaData(string path, Action<MetaData> success, Action<DropboxException> failure)
		{
			ExecuteDropboxAction(
				() => _dropNetClient.GetMetaDataAsync(path, success, WrapExceptionHandler(failure)));
		}

		public void Upload(string path, string filename, byte[] bytes, Action<RestResponse> success,
		                   Action<DropboxException> failure)
		{
			ExecuteDropboxAction(
				() => _dropNetClient.UploadFileAsync(path, filename, bytes, success, WrapExceptionHandler(failure)));
		}

		public void GetFile(string path, Action<RestResponse> success, Action<DropboxException> failure)
		{
			ExecuteDropboxAction(
				() => _dropNetClient.GetFileAsync(path, success, WrapExceptionHandler(failure)));
		}
	}
}