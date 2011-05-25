using System;
using todotxtlib.net;

namespace TodotxtTouch.WindowsPhone.ViewModel
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

		public override string Description
		{
			get { return _context; }
		}

		public override string Target
		{
			get { return _context; }
		}
	}
}