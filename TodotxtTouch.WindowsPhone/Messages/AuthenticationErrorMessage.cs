using System;

namespace TodotxtTouch.WindowsPhone.Messages
{
	internal class AuthenticationErrorMessage
	{
		public AuthenticationErrorMessage(Exception exception)
		{
			Exception = exception;
		}

		public Exception Exception { get; }
	}
}