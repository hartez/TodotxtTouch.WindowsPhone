using System;

namespace TodotxtTouch.WindowsPhone.Messages
{
	// TODO hartez 2017/06/04 11:35:12 Better name for this class	
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