namespace TodotxtTouch.WindowsPhone.Messages
{
	internal class DrillDownMessage
	{
		public string Filter { get; private set; }

		public DrillDownMessage(string filter)
		{
			Filter = filter;
		}
	}
}