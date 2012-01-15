using System;

namespace TodotxtTouch.WindowsPhone.Messages
{
	public class RetrievedDropboxTokenMessage
	{
		public RetrievedDropboxTokenMessage(string error)
		{
			Error = error;
		}

		public RetrievedDropboxTokenMessage(Uri tokenUri)
		{
			TokenUri = tokenUri;
		}

		public string Error { get; set; }
		public Uri TokenUri { get; set; }
	}
}