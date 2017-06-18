using System;
using todotxtlib.net;

namespace TodotxtTouch.WindowsPhone.Tasks
{
	public class ContextTaskFilter : TaskFilter
	{
		private readonly string _context;

		public ContextTaskFilter(Func<Task, bool> filter, string context) 
			: base(filter)
		{
			_context = context;
		}

		public override string ToString()
		{
			return "context:" + _context;
		}

		public override string Description => _context;

		public override string Target => _context;
	}
}