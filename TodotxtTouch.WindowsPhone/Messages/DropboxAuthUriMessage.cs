using System;

namespace TodotxtTouch.WindowsPhone.Messages
{
	public class DropboxAuthUriMessage
	{
		public DropboxAuthUriMessage(string error)
		{
			Error = error;
		}

		public DropboxAuthUriMessage(Uri tokenUri)
		{
			TokenUri = tokenUri;
		}

		public string Error { get; set; }
		public Uri TokenUri { get; set; }
	}
}