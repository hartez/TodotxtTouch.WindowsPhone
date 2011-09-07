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
				_getVersion = () => { return DesignVersion; };
			}
			else
			{
				_getVersion = () => Assembly.GetExecutingAssembly().FullName.Split('=')[1].Split(',')[0];
			}
		}

		public string AppVersion
		{
			get { return String.Format("Version {0}", _getVersion()); }
		}

		public string ApplicationTitle
		{
			get { return "Todo.txt"; }
		}

		public string SupportWebsite
		{
			get { return "http://todotxt.traceur-llc.com"; }
		}

		public string SupportEmail
		{
			get { return "support@traceur-llc.com"; }
		}
	}
}