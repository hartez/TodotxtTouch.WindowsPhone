using System;
using System.Net.NetworkInformation;
using System.Security.Cryptography;

namespace TodotxtTouch.WindowsPhone.Service
{
	public class NetworkHelper
	{
		// Default: use the normal method for checking
		private static Func<bool> _isNetworkAvailable = NetworkInterface.GetIsNetworkAvailable;

		/// <summary>
		/// Specify a custom method for determining if the network is available.
		/// </summary>
		/// <param name="isNetworkAvailable">The method to determine whether the network is considered 'available'</param>
		public static void Initialize(Func<bool> isNetworkAvailable)
		{
			_isNetworkAvailable = isNetworkAvailable;
		}

		public static bool GetIsNetworkAvailable()
		{
			return _isNetworkAvailable();
		}

		/// <summary>
		/// The network is never available
		/// </summary>
		public static void TestNeverAvailable()
		{
			Initialize(() => false);
		}

		// The network is randomly available
		public static void TestChaoticallyAvailable()
		{
			Initialize(() =>
				{
					var r = new RNGCryptoServiceProvider();
					var number = new byte[1];
					r.GetBytes(number);

					return number[0]%2 == 0;
				});
		}
	}
}