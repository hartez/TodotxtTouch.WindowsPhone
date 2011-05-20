using System.Collections.Generic;
using System.Linq;
using todotxtlib.net;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
	public static class TaskListExtensions
	{
		public static IEnumerable<Task> ApplyFilters(this IEnumerable<Task> taskList, IEnumerable<TaskFilter> filters)
		{
			IEnumerable<Task> filterResults = taskList.AsEnumerable();

			return filters.Aggregate(filterResults, (current, taskFilter) => current.Where(taskFilter.Filter));
		}
	}
}