using System;
using System.Collections.Generic;
using System.Linq;
using EZLibrary;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
	public class TaskFilterFactory
	{
		private const char Delimiter = ',';

		public static TaskFilter CreateTaskFilterFromString(string filter)
		{
			if (filter.StartsWith("context:"))
			{
				string target = filter.Replace("context:", String.Empty);
				return new ContextTaskFilter(
					task => task.Contexts.Contains(target),
					target);
			}

			if (filter.StartsWith("project:"))
			{
				string target = filter.Replace("project: ", "+");
				return new ContextTaskFilter(
					task => task.Projects.Contains(target),
					target);
			}

			return null;
		}

		public static List<TaskFilter> ParseFilterString(string filter)
		{
			string[] filters = filter.Split(Delimiter);

			return filters.Where(f => !String.IsNullOrEmpty(f)).Select(CreateTaskFilterFromString).ToList();
		}

		public static string CreateFilterString(List<TaskFilter> filters)
		{
			return filters.ToDelimitedList(t => t.ToString(), Delimiter.ToString());
		}
	}
}