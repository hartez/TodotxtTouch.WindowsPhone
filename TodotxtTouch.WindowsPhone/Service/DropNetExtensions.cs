using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Resources;
using Dropbox.Api;
using Newtonsoft.Json;

namespace TodotxtTouch.WindowsPhone.Service
{
	public static class DropNetExtensions
	{
		//public static DropboxClient CreateClient()
		//{
		//	var apikeys = LoadApiKeysFromFile();

		//	return new DropboxClient(apikeys["dropboxkey"], apikeys["dropboxsecret"]);
		//}

		public static DropboxClient CreateClient(string token, string secret)
		{
			//var apikeys = LoadApiKeysFromFile();

			return new DropboxClient(token, new DropboxClientConfig("TodotxtTouch.WindowsPhone"));

			//return new DropboxClient(apikeys["dropboxkey"], apikeys["dropboxsecret"], token, secret);
		}

		public static Dictionary<string, string> LoadApiKeysFromFile()
		{
			StreamResourceInfo apikeysResource =
				Application.GetResourceStream(new Uri("/TodotxtTouch.WindowsPhone;component/apikeys.txt", UriKind.Relative));

			var sr = new StreamReader(apikeysResource.Stream);

			string keys = sr.ReadToEnd();

			JsonSerializer serializer = JsonSerializer.Create(new JsonSerializerSettings());

			var reader = new JsonTextReader(new StringReader(keys));

			return serializer.Deserialize<Dictionary<string, string>>(reader);
		}
	}
}