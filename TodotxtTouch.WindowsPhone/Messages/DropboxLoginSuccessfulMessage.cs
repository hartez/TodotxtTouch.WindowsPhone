using System;

namespace TodotxtTouch.WindowsPhone.Messages
{
	public class DropboxLoginSuccessfulMessage
	{
		public DropboxLoginSuccessfulMessage(Uri redirectUri)
		{
			RedirectUri = redirectUri;
		}

		public Uri RedirectUri{ get; }
	}
}