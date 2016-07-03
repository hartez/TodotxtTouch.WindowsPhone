using System.Collections.Generic;
using System.Linq;

namespace TodotxtTouch.WindowsPhone.Tasks
{
	public static class TaskFilterExtensions
	{
		public static string CreateDefaultBodyText(this List<TaskFilter> filters)
		{
			return filters.Aggregate(string.Empty, 
			                         (body, filter) => body + (body.Length == 0 ? string.Empty : " ") + filter.Target);
		}

	}
}