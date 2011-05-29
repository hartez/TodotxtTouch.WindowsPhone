using System;
using todotxtlib.net;

namespace TodotxtTouch.WindowsPhone.Tasks
{
	public abstract class TaskFilter
	{
		protected TaskFilter(Func<Task, bool> filter)
		{
			Filter = filter;
		}

		public Func<Task, bool> Filter { get; private set; }
		public abstract String Description { get; }

		public abstract String Target { get; }
	}
}