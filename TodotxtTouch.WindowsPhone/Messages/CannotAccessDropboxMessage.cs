namespace TodotxtTouch.WindowsPhone.Messages
{
	public class CannotAccessDropboxMessage
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