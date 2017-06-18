using System;
using todotxtlib.net;

namespace TodotxtTouch.WindowsPhone.Tasks
{
	public class ProjectTaskFilter : TaskFilter
	{
		private readonly string _project;

		public ProjectTaskFilter(Func<Task, bool> filter, string project) 
			: base(filter)
		{
			_project = project;
		}
		
		public override string ToString()
		{
			return "project:" + _project;
		}

		public override string Description => _project;

		public override string Target => _project;
	}
}