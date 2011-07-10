namespace TodotxtTouch.WindowsPhone.ViewModel
{
	public class ApplicationSettingsChangedMessage
	{
		public ApplicationSettingsChangedMessage(ApplicationSettings settings)
		{
			Settings = settings;
		}

		public ApplicationSettings Settings { get; set; }
	}
}