/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocatorTemplate xmlns:vm="clr-namespace:TodotxtTouch.WindowsPhone"
                                   x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"

  You can also use Blend to do all this with the tool's support.
  See http://www.galasoft.ch/mvvm
*/

using System.Diagnostics.CodeAnalysis;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using TodotxtTouch.WindowsPhone.Messages;
using TodotxtTouch.WindowsPhone.Service;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
	/// <summary>
	/// This class contains static references to all the view models in the
	/// application and provides an entry point for the bindings.
	/// </summary>
	public class ViewModelLocator
	{
		private static MainViewModel _main;
		private static AboutViewModel _about;
		private static DropboxCredentialsViewModel _dropBoxCredentials;
		private static ApplicationSettingsViewModel _applicationSettingsViewModel;
		private static DropboxService _dropBoxService;

		/// <summary>
		/// Initializes a new instance of the ViewModelLocator class.
		/// </summary>
		public ViewModelLocator()
		{
			_about = new AboutViewModel();

			if (ViewModelBase.IsInDesignModeStatic)
			{
			    // Create design time services and viewmodels
                _applicationSettingsViewModel = new ApplicationSettingsViewModel(new ApplicationSettings(), null);
				_main = new MainViewModel(null, null, null);
				_about = new AboutViewModel();
			}
			else
			{
				var settings = new ApplicationSettings();
				_dropBoxService = new DropboxService(settings);

				_applicationSettingsViewModel = new ApplicationSettingsViewModel(settings, _dropBoxService);

				Messenger.Default.Register<ApplicationSettingsChangedMessage>(this, asc => Initialize(asc.Settings));

				_dropBoxCredentials = new DropboxCredentialsViewModel(_dropBoxService);

				Initialize(settings);
			}
		}

		/// <summary>
		/// Gets the Main property which defines the main viewmodel.
		/// </summary>
		[SuppressMessage("Microsoft.Performance",
			"CA1822:MarkMembersAsStatic",
			Justification = "This non-static member is needed for data binding purposes.")]
		public MainViewModel Main => _main;

		public AboutViewModel About => _about;

		/// <summary>
		/// Gets the ApplicationSettings property which defines the dropbox credentials viewmodel.
		/// </summary>
		[SuppressMessage("Microsoft.Performance",
			"CA1822:MarkMembersAsStatic",
			Justification = "This non-static member is needed for data binding purposes.")]
		public DropboxCredentialsViewModel DropboxCredentials => _dropBoxCredentials;

		/// <summary>
		/// Gets the ApplicationSettings property which defines the application settings viewmodel.
		/// </summary>
		[SuppressMessage("Microsoft.Performance",
			"CA1822:MarkMembersAsStatic",
			Justification = "This non-static member is needed for data binding purposes.")]
		public ApplicationSettingsViewModel ApplicationSettings => _applicationSettingsViewModel;

		private static void Initialize(ApplicationSettings settings)
		{
			var ptfs = new PrimaryTaskFileService(_dropBoxService, settings);
			var atfs = new ArchiveTaskFileService(_dropBoxService, settings);

			if (_main == null)
			{
				_main = new MainViewModel(ptfs, atfs, settings);
			}
			else
			{
				_main.WireupTaskFileServices(ptfs, atfs);
			}
		}
	}
}