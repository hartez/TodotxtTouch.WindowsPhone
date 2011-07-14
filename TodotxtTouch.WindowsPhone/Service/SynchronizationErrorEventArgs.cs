using System;

namespace TodotxtTouch.WindowsPhone.Service
{
	public class SynchronizationErrorEventArgs : EventArgs
	{
		public Exception Exception { get; private set; }

		public SynchronizationErrorEventArgs(Exception exception)
		{
			Exception = exception;
		}
	}
}