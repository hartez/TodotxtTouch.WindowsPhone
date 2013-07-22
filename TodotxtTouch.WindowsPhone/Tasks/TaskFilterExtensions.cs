using System;
using System.Collections.Generic;
using System.Linq;

namespace TodotxtTouch.WindowsPhone.Tasks
{
	public static class TaskFilterExtensions
	{
		public static string CreateDefaultBodyText(this List<TaskFilter> filters)
		{
			return filters.Aggregate(String.Empty, 
			                         (body, filter) => body + (body.Length == 0 ? String.Empty : " ") + filter.Target);
		}

	}
}