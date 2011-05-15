using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Resources;
using DropNet;
using Newtonsoft.Json;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
	public static class DropNetExtensions
	{
		public static DropNetClient CreateClient()
		{
			var apikeys = LoadApiKeysFromFile();

			return new DropNetClient(apikeys["dropboxkey"], apikeys["dropboxsecret"]);
		}

		public static DropNetClient CreateClient(string token, string secret)
		{
			var apikeys = LoadApiKeysFromFile();

			return new DropNetClient(apikeys["dropboxkey"], apikeys["dropboxsecret"], token, secret);
		}

		private static Dictionary<string, string> LoadApiKeysFromFile()
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