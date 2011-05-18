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

using System.Collections.ObjectModel;
using GalaSoft.MvvmLight;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    public class ViewModelLocator
    {
        private static MainViewModel _main;
    	private static DropBoxCredentialsViewModel _dropBoxCredentials;

        /// <summary>
        /// Initializes a new instance of the ViewModelLocator class.
        /// </summary>
        public ViewModelLocator()
        {
            ////if (ViewModelBase.IsInDesignModeStatic)
            ////{
            ////    // Create design time services and viewmodels
            ////}
            ////else
            ////{
            ////    // Create run time services and view models
            ////}

			_dropBoxCredentials = new DropBoxCredentialsViewModel();
            _main = new MainViewModel(_dropBoxCredentials);
        }

        /// <summary>
        /// Gets the Main property which defines the main viewmodel.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification = "This non-static member is needed for data binding purposes.")]
        public MainViewModel Main
        {
            get
            {
                return _main;
            }
        }

		/// <summary>
		/// Gets the ApplicationSettings property which defines the application settings viewmodel.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance",
			"CA1822:MarkMembersAsStatic",
			Justification = "This non-static member is needed for data binding purposes.")]
		public DropBoxCredentialsViewModel DropBoxCredentials
		{
			get
			{
				return _dropBoxCredentials;
			}
		}

    }
}