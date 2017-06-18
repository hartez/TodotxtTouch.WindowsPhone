﻿using System.Collections.Generic;
using System.Linq;
using todotxtlib.net;

namespace TodotxtTouch.WindowsPhone.Tasks
{
	public static class TaskListExtensions
	{
		public static IEnumerable<Task> ApplyFilters(this IEnumerable<Task> taskList, IEnumerable<TaskFilter> filters)
		{
			IEnumerable<Task> filterResults = taskList.AsEnumerable();

			return filters.Aggregate(filterResults, (current, taskFilter) => current.Where(taskFilter.Filter));
		}

		public static IEnumerable<Task> ApplySorts(this IEnumerable<Task> taskList)
		{
			return taskList.OrderByDescending(task => task.IsPriority)
				.ThenBy(task => task.Priority)
				.ThenBy(task => task.Completed)
				.ThenBy(task => string.IsNullOrEmpty(task.Body) ? string.Empty : task.Body.ToLower()); 
		}
	}
}