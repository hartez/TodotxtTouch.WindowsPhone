using System;

namespace TodotxtTouch.WindowsPhone.Messages
{
	internal class SynchronizationErrorMessage
	{
		public Exception Exception { get; private set; }

		public SynchronizationErrorMessage(Exception exception)
		{
			Exception = exception;
		}
	}
}