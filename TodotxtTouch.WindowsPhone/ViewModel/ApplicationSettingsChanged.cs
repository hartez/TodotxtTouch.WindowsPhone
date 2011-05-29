namespace TodotxtTouch.WindowsPhone.ViewModel
{
	public class ApplicationSettingsChanged
	{
		public ApplicationSettingsChanged(ApplicationSettings settings)
		{
			Settings = settings;
		}

		public ApplicationSettings Settings { get; set; }
	}
}