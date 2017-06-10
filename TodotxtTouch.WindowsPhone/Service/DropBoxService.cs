using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using Dropbox.Api;
using Dropbox.Api.Files;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using TodotxtTouch.WindowsPhone.Messages;

namespace TodotxtTouch.WindowsPhone.Service
{
	public class DropboxService
	{
		private DropboxClient _dropNetClient;

		private const string DropboxApiKey = "dropboxkey";

		private string _secret = string.Empty;
		private string _token = string.Empty;
		private string _oauth2State;

		public bool WeHaveTokens => !string.IsNullOrEmpty(Token);// && !string.IsNullOrEmpty(Secret);

		public void Disconnect()
	    {
	        Token = string.Empty;
	        Secret = string.Empty;
	    }

	    /// <summary>
		/// Gets the Secret property.
		/// Changes to that property's value raise the PropertyChanged event. 
		/// </summary>
		public string Secret
		{
			get
			{
				if (string.IsNullOrEmpty(_secret))
				{
					string secret;
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
				if (string.IsNullOrEmpty(_token))
				{
					string token;
					if (IsolatedStorageSettings.ApplicationSettings.TryGetValue("dropboxToken", out token))
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

		private Task<DropboxClient> Auth()
		{
			TaskCompletionSource<DropboxClient> tcs = new TaskCompletionSource<DropboxClient>();

			Messenger.Default.Register<CredentialsUpdatedMessage>(
					this, async (message) =>
					{
						Messenger.Default.Unregister<CredentialsUpdatedMessage>(this);
						tcs.SetResult(await Client().ConfigureAwait(false));
					});

			Messenger.Default.Send(new NeedCredentialsMessage("Not authenticated"));

			return tcs.Task;
		}

		private async Task<DropboxClient> Client()
		{
			if (_dropNetClient != null)
			{
				return _dropNetClient;
			}

			if (WeHaveTokens)
			{
				_dropNetClient = DropNetExtensions.CreateClient(Token, Secret);
				return _dropNetClient;
			}

			return await Auth();
		}

		//private async Task EnsureDropboxClient()
		//{
			

		//	Messenger.Default.Register<CredentialsUpdatedMessage>(
		//			this, (message) =>
		//			{
		//				Messenger.Default.Unregister<CredentialsUpdatedMessage>(this);
		//				EnsureDropboxClient();
		//			});

		//	Messenger.Default.Send(new NeedCredentialsMessage("Not authenticated"));
		//}

		private void ExecuteDropboxAction(Action dropboxAction)
		{
			if (WeHaveTokens)
			{
				if (_dropNetClient == null)
				{
					_dropNetClient = DropNetExtensions.CreateClient(Token, Secret);
				}

				dropboxAction?.Invoke();
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

					var httpException = ex as HttpException;

				    if(httpException != null)
				    {
				        // Dropnet responds with BadGateway if the network isn't accessible
				        switch((HttpStatusCode)httpException.StatusCode)
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

					handler?.Invoke(ex);
				};
		}

		// TODO hartez 2017/06/04 13:57:31 Fix names with Async suffix	
		public async Task<Metadata> GetMetaData(string path)
		{
			var client = await Client();
			return await client.Files.GetMetadataAsync(new GetMetadataArg(path));
		}

		public async Task<Metadata> Upload(string path, string filename, byte[] bytes)
		{
			using (var stream = new MemoryStream(bytes))
			{
				return await Client().Result.Files.UploadAsync(new CommitInfo(path + "/" + filename), stream);
			}
		}

		public async Task<string> GetFile(string path)
		{
			var response = await Client().Result.Files.DownloadAsync(new DownloadArg(path));
			return await response.GetContentAsStringAsync();
		}

		public void StartLoginProcess()
		{
			try
			{
				var keys = DropNetExtensions.LoadApiKeysFromFile();

				if (!keys.ContainsKey(DropboxApiKey) || String.IsNullOrEmpty(keys[DropboxApiKey]))
				{
					throw new Exception("Missing Dropbox API key");
				}

				_oauth2State = Guid.NewGuid().ToString("N");

				var authUri = DropboxOAuth2Helper.GetAuthorizeUri(OAuthResponseType.Token, keys[DropboxApiKey],
					redirectUri: new Uri("https://www.codewise-llc.com/todotxtoauth2"), state: _oauth2State);

				Messenger.Default.Send(new RetrievedDropboxTokenMessage(authUri));
			}
			catch (Exception ex)
			{
				Messenger.Default.Send(new RetrievedDropboxTokenMessage(ex.Message));
			}
		}

		public void GetAccessToken(DropboxLoginSuccessfulMessage msg)
		{
			// TODO hartez 2017/06/04 11:52:12 long term, move this message.show stuff to a generic error message handler in the UI; this service shouldn't be doing message.show	


			try
			{
				OAuth2Response result = DropboxOAuth2Helper.ParseTokenFragment(msg.RedirectUri);
				if (result.State != _oauth2State)
				{
					// TODO hartez 2017/06/04 11:47:24 Should this be displaying some sort of error? 	
					//DispatcherHelper.CheckBeginInvokeOnUI(() => MessageBox.Show(error.Message));
					return;
				}

				Token = result.AccessToken;
				Messenger.Default.Send(new CredentialsUpdatedMessage());
			}
			catch (Exception ex)
			{
				DispatcherHelper.CheckBeginInvokeOnUI(() => MessageBox.Show(ex.Message));
				//throw;
			}
		}
	}
}