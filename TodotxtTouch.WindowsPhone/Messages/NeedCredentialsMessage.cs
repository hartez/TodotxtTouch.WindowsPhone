namespace TodotxtTouch.WindowsPhone.Messages
{
	public class NeedCredentialsMessage
	{
		public string Reason { get; private set; }

		public NeedCredentialsMessage(string reason)
		{
			Reason = reason;
		}
	}
}