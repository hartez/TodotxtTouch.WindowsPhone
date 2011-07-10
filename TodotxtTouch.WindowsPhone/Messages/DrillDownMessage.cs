namespace TodotxtTouch.WindowsPhone.Messages
{
	public class DrillDownMessage
	{
		public string Filter { get; private set; }

		public DrillDownMessage(string filter)
		{
			Filter = filter;
		}
	}
}