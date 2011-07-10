using TodotxtTouch.WindowsPhone.ViewModel;

namespace TodotxtTouch.WindowsPhone.Messages
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