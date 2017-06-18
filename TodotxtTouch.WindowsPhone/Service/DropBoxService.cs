using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Resources;
using Dropbox.Api;
using Dropbox.Api.Files;
using Dropbox.Api.Stone;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using TodotxtTouch.WindowsPhone.Messages;
using TodotxtTouch.WindowsPhone.ViewModel;

namespace TodotxtTouch.WindowsPhone.Service
{
	public class DropboxService
	{
		private readonly ApplicationSettings _settings;
		private DropboxClient _dropboxClient;

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
				_dropboxClient = new DropboxClient(_settings.Token, new DropboxClientConfig("TodotxtTouch.WindowsPhone"));
				return _dropboxClient;
			}

			return await Authenticate();
		}

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

		private static string LoadApiKeyFromFile()
		{
			StreamResourceInfo apikeysResource =
				Application.GetResourceStream(new Uri("/TodotxtTouch.WindowsPhone;component/apikeys.txt", UriKind.Relative));

			var sr = new StreamReader(apikeysResource.Stream);

			string keys = sr.ReadToEnd();

			JsonSerializer serializer = JsonSerializer.Create(new JsonSerializerSettings());

			var reader = new JsonTextReader(new StringReader(keys));

			return serializer.Deserialize<Dictionary<string, string>>(reader)["dropboxkey"];
		}

		public void StartLoginProcess()
		{
			try
			{
				var dropboxkey = LoadApiKeyFromFile();

				if (string.IsNullOrEmpty(dropboxkey))
				{
					throw new Exception("Missing Dropbox API key");
				}

				_oauth2State = Guid.NewGuid().ToString("N");

				var authUri = DropboxOAuth2Helper.GetAuthorizeUri(OAuthResponseType.Token, dropboxkey,
					redirectUri: new Uri("https://www.codewise-llc.com/todotxtoauth2"), state: _oauth2State);

				Messenger.Default.Send(new DropboxAuthUriMessage(authUri));
			}
			catch (Exception ex)
			{
				Messenger.Default.Send(new DropboxAuthUriMessage(ex.Message));
			}
		}

		public void GetAccessToken(DropboxLoginSuccessfulMessage msg)
		{
			try
			{
				OAuth2Response result = DropboxOAuth2Helper.ParseTokenFragment(msg.RedirectUri);
				if (result.State != _oauth2State)
				{
					throw new Exception("OAuth2 state mismatch");
				}

				_settings.Token = result.AccessToken;
				Messenger.Default.Send(new CredentialsUpdatedMessage());
			}
			catch (Exception ex)
			{
				Messenger.Default.Send(new AuthenticationErrorMessage(ex));
			}
		}
	}
}