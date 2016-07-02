using System;
using System.Reflection;
using GalaSoft.MvvmLight;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
    public class AboutViewModel : ViewModelBase
	{
		private const string DesignVersion = "1.2.3.4";

		private readonly Func<string> _getVersion;

		public AboutViewModel()
		{
			if (IsInDesignMode)
			{
				_getVersion = () => DesignVersion;
			}
			else
			{
				_getVersion = () => Assembly.GetExecutingAssembly().FullName.Split('=')[1].Split(',')[0];
			}
		}

		public string AppVersion => $"Version {_getVersion()}";

	    public string ApplicationTitle => "Todo.txt";

	    public string SupportWebsite => "http://todotxt.codewise-llc.com";

	    public string SupportEmail => "support@codewise-llc.com";

	    public string AppLongTitle => "Todo.txt for Windows Phone";

	    public string Copyright => $"© 2012-{DateTime.Now.Year} CodeWise LLC, All Rights Reserved";
	}
}