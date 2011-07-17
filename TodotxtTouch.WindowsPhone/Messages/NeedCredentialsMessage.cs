namespace TodotxtTouch.WindowsPhone.Messages
{
	internal class NeedCredentialsMessage
	{
		public string Reason { get; private set; }

		public NeedCredentialsMessage(string reason)
		{
			Reason = reason;
		}
	}
}