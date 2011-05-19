using System;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
	public class LoadingStateChangedEventArgs : EventArgs
	{
		public TaskLoadingState LoadingState { get; private set; }

		public LoadingStateChangedEventArgs(TaskLoadingState loadingState)
		{
			LoadingState = loadingState;
		}
	}
}