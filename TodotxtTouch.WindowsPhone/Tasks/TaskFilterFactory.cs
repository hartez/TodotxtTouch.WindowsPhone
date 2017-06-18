using System.Collections.Generic;
using System.Linq;
using EZLibrary;

namespace TodotxtTouch.WindowsPhone.Tasks
{
	public class TaskFilterFactory
	{
		private const char Delimiter = ',';

		public static TaskFilter CreateTaskFilterFromString(string filter)
		{
			if (filter.StartsWith("context:"))
			{
				var target = filter.Replace("context:", string.Empty);
				return new ContextTaskFilter(
					task => task.Contexts.Contains(target),
					target);
			}

			if (filter.StartsWith("project:"))
			{
				var target = filter.Replace("project: ", "+");
				return new ProjectTaskFilter(
					task => task.Projects.Contains(target),
					target);
			}

			return null;
		}

		public static List<TaskFilter> ParseFilterString(string filter)
		{
			var filters = filter.Split(Delimiter);

			return filters.Where(f => !string.IsNullOrEmpty(f)).Select(CreateTaskFilterFromString).ToList();
		}

		public static string CreateFilterString(List<TaskFilter> filters)
		{
			return filters.ToDelimitedList(t => t.ToString(), Delimiter.ToString());
		}
	}
}