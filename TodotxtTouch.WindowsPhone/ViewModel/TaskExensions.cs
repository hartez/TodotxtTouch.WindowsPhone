using todotxtlib.net;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
	public static class TaskExensions
	{
		public static Task Copy(this Task task)
		{
			return new Task(task.Raw);
		}
	}
}