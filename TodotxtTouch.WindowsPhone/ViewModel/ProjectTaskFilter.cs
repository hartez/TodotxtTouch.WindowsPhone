using System;
using todotxtlib.net;

namespace TodotxtTouch.WindowsPhone.ViewModel
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

		public override string Description
		{
			get { return _project; }
		}

		public override string Target
		{
			get { return _project; }
		}
	}
}