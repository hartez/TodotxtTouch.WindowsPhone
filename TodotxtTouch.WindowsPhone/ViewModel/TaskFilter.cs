using System;
using todotxtlib.net;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
	public class TaskFilter
	{
		public TaskFilter(Func<Task, bool> filter, string description)
		{
			Filter = filter;
			Description = description;
		}

		public Func<Task, bool> Filter { get; private set; }
		public String Description { get; private set; }
	}
}