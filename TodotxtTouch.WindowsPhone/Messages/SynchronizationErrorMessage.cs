using System;

namespace TodotxtTouch.WindowsPhone.Messages
{
	public class SynchronizationErrorMessage
	{
		public Exception Exception { get; private set; }

		public SynchronizationErrorMessage(Exception exception)
		{
			Exception = exception;
		}
	}
}