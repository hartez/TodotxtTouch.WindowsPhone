namespace TodotxtTouch.WindowsPhone.ViewModel
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