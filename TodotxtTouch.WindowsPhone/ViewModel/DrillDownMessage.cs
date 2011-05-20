namespace TodotxtTouch.WindowsPhone.ViewModel
{
	public class DrillDownMessage
	{
		public string Depth { get; private set; }

		public DrillDownMessage(string depth)
		{
			Depth = depth;
		}
	}

	public class DrillUpMessage
	{}
}