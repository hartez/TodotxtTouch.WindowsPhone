using System;
using System.IO.IsolatedStorage;
using System.Net;
using System.Windows;
using DropNet;
using DropNet.Exceptions;
using DropNet.Models;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using RestSharp;
using TodotxtTouch.WindowsPhone.Messages;

namespace TodotxtTouch.WindowsPhone.Service
{
	public class DropboxService
	{
		private DropNetClient _dropNetClient;

		private String _secret = String.Empty;
		private String _token = String.Empty;

		public bool WeHaveTokens
		{
			get { return !String.IsNullOrEmpty(Token) && !String.IsNullOrEmpty(Secret); }
		}

	    public void Disconnect()
	    {
	        Token = String.Empty;
	        Secret = String.Empty;
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
				    if(ex == null)
				    {
                        Messenger.Default.Send(new CannotAccessDropboxMessage("Dropbox is inaccessible; no error information available."));
				        return;
				    }

				    if(ex.Response != null)
				    {
				        // Dropnet responds with BadGateway if the network isn't accessible
				        switch(ex.Response.StatusCode)
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
				                Messenger.Default.Send(new NeedCredentialsMessage("Authentication failed"));
				                break;
				            case HttpStatusCode.BadRequest:
				                Messenger.Default.Send(new CannotAccessDropboxMessage("Lacking mobile authentication permission"));
				                break;
				            default:
				                Messenger.Default.Send(new CannotAccessDropboxMessage());
				                break;
				        }
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

		public void Upload(string path, string filename, byte[] bytes, Action<MetaData> success,
		                   Action<DropboxException> failure)
		{
			ExecuteDropboxAction(
				() => _dropNetClient.UploadFileAsync(path, filename, bytes, success, WrapExceptionHandler(failure)));
		}

		public void GetFile(string path, Action<IRestResponse> success, Action<DropboxException> failure)
		{
			ExecuteDropboxAction(
				() => _dropNetClient.GetFileAsync(path, success, WrapExceptionHandler(failure)));
		}

		public void GetToken()
		{
			if (_dropNetClient == null)
			{
				_dropNetClient = DropNetExtensions.CreateClient();
			}

			_dropNetClient.GetTokenAsync(
				success =>
				{
					string tokenUrl = _dropNetClient.BuildAuthorizeUrl("http://todotxt.traceur-llc.com/dblogin.htm");

					Messenger.Default.Send(new RetrievedDropboxTokenMessage(new Uri(tokenUrl)));
				},
			    failure => Messenger.Default.Send(new RetrievedDropboxTokenMessage(failure.Message)));
		}

		public void GetAccessToken()
		{
			_dropNetClient.GetAccessTokenAsync(response =>
				{
					Token = response.Token;
					Secret = response.Secret;
					Messenger.Default.Send(new CredentialsUpdatedMessage());
				},
			error => DispatcherHelper.CheckBeginInvokeOnUI(
				() => MessageBox.Show(error.Message)));
		}
	}
}