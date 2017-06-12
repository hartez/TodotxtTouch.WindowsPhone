using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using Dropbox.Api;
using Dropbox.Api.Files;
using Dropbox.Api.Stone;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using TodotxtTouch.WindowsPhone.Messages;
using TodotxtTouch.WindowsPhone.ViewModel;

namespace TodotxtTouch.WindowsPhone.Service
{
	public class DropboxService
	{
		private readonly ApplicationSettings _settings;
		private DropboxClient _dropboxClient;

		private const string DropboxApiKey = "dropboxkey";

		public DropboxService(ApplicationSettings settings)
		{
			_settings = settings;
		}

		private string _oauth2State;

		public bool WeHaveTokens => !string.IsNullOrEmpty(_settings.Token);

		public void Disconnect()
	    {
			_settings.Token = string.Empty;
	    } 

		private Task<DropboxClient> Authenticate()
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
			if (_dropboxClient != null)
			{
				return _dropboxClient;
			}

			if (WeHaveTokens)
			{
				_dropboxClient = DropNetExtensions.CreateClient(_settings.Token);
				return _dropboxClient;
			}

			return await Authenticate();
		}

		// TODO hartez 2017/06/11 17:45:48 Update this using the code from PneumaticTube	
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
				                _dropboxClient = null;
								_settings.Token = string.Empty;
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


		public async Task<Metadata> GetMetaDataAsync(string path)
		{
			try
			{
				var client = await Client();
				return await client.Files.GetMetadataAsync(new GetMetadataArg(path));
			}
			catch (ApiException<GetMetadataError>)
			{
				// Path not found; the file's not in Dropbox yet
			}

			return null;
		}

		public async Task<Metadata> UploadAsync(string path, string filename, string revision, byte[] bytes)
		{
			using (var stream = new MemoryStream(bytes))
			{
				var writeMode = revision == null ? WriteMode.Add.Instance as WriteMode : new WriteMode.Update(revision);

				return await Client().Result.Files.UploadAsync(new CommitInfo(path + "/" + filename, writeMode), stream);
			}
		}

		public async Task<IDownloadResponse<FileMetadata>> GetFileAsync(string path)
		{
			return await Client().Result.Files.DownloadAsync(new DownloadArg(path));
		}

		public void StartLoginProcess()
		{
			try
			{
				var keys = DropNetExtensions.LoadApiKeysFromFile();

				if (!keys.ContainsKey(DropboxApiKey) || string.IsNullOrEmpty(keys[DropboxApiKey]))
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

				_settings.Token = result.AccessToken;
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