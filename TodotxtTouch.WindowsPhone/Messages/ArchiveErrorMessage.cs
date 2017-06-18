using System;

namespace TodotxtTouch.WindowsPhone.Messages
{
	internal class ArchiveErrorMessage
	{
		public Exception Exception { get; private set; }

		public ArchiveErrorMessage(Exception exception)
		{
			Exception = exception;
		}
	}
}