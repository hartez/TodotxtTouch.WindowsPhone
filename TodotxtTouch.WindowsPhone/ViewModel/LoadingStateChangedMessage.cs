namespace TodotxtTouch.WindowsPhone.ViewModel
{
	public class LoadingStateChangedMessage
	{
		public LoadingStateChangedMessage(TaskLoadingState newState)
		{
			_state = newState;
		}

		private TaskLoadingState _state;
		public TaskLoadingState State
		{
			get { return _state; }
			set { _state = value; }
		}
	}
}