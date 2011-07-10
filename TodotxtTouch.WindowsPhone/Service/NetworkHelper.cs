using System;
using System.Net.NetworkInformation;

namespace TodotxtTouch.WindowsPhone.Service
{
	public class NetworkHelper
	{
		private static Func<bool> _isNetworkAvailable = NetworkInterface.GetIsNetworkAvailable;

		public static void Intialize(Func<bool> isNetworkAvailable)
		{
			_isNetworkAvailable = isNetworkAvailable;
		}

		public static bool GetIsNetworkAvailable()
		{
			return _isNetworkAvailable();
		}
	}
}