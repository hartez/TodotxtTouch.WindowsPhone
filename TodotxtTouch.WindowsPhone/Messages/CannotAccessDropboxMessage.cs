namespace TodotxtTouch.WindowsPhone.Messages
{
	internal class CannotAccessDropboxMessage
	{
		public string Reason { get; private set; }

		public CannotAccessDropboxMessage()
		{
			Reason = "Unknown error";
		}

		public CannotAccessDropboxMessage(string reason)
		{
			Reason = reason;
		}
	}
}