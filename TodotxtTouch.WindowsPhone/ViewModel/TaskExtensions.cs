using todotxtlib.net;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
	public static class TaskExtensions
	{
		public static Task Copy(this Task task)
		{
			return new Task(task.ToString());
		}

		public static void UpdateTo(this Task task, Task newTask)
		{
			task.Body = newTask.Body;
			task.Completed = newTask.Completed;
			task.Priority = newTask.Priority;
		}
	}
}