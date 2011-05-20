using todotxtlib.net;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
	public static class TaskExtensions
	{
		public static Task Copy(this Task task)
		{
			return new Task(task.Raw);
		}
	}
}