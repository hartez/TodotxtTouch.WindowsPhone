using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EZLibrary;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
	public class TaskFilterFactory
	{
		const char Delimiter = ',';

		public static TaskFilter CreateTaskFilterFromString(string filter)
		{
			if(filter.StartsWith("context:"))
			{
				var target = filter.Replace("context:", String.Empty);
				return new ContextTaskFilter(
					task => task.Contexts.Contains(target),
					target);
			}
			
			if (filter.StartsWith("project:"))
			{
				var target = filter.Replace("project:", String.Empty);
				return new ContextTaskFilter(
					task => task.Projects.Contains(target),
					target );
			}

			return null;
		}

		public static List<TaskFilter> ParseFilterString(string filter)
		{
			var filters = filter.Split(Delimiter);

			return filters.Where(f => !String.IsNullOrEmpty(f)).Select(CreateTaskFilterFromString).ToList();
		}

		public static string CreateFilterString(List<TaskFilter> filters)
		{
			return filters.ToDelimitedList(t => t.ToString(), Delimiter.ToString());
		}
	}
}