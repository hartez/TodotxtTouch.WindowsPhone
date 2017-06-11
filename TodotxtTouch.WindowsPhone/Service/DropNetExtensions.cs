using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Resources;
using Dropbox.Api;
using Newtonsoft.Json;

namespace TodotxtTouch.WindowsPhone.Service
{
	// TODO hartez 2017/06/11 17:45:10 Rename this class	
	public static class DropNetExtensions
	{
		public static DropboxClient CreateClient(string token)
		{
			return new DropboxClient(token, new DropboxClientConfig("TodotxtTouch.WindowsPhone"));
		}

		// TODO hartez 2017/06/11 17:44:21 Is this literally the only place where we're using Newtonsoft? Might be worth removing that dependency 	
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