using System;
using System.Collections.Generic;
using System.Linq;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
	public static class StringExtensions
	{
		public static String ToCommaDelimitedList<T>(this IEnumerable<T> enumerable, Func<T, String> stringSelector)
		{
			return ToDelimitedList(enumerable, stringSelector, ", ");
		}

		public static String ToDelimitedList<T>(this IEnumerable<T> enumerable, Func<T, String> stringSelector, String delimiter)
		{
			return enumerable.Aggregate(String.Empty,
			                            (list, thing) => list
			                                             + (list.Length == 0 ? String.Empty : delimiter)
			                                             + stringSelector(thing));
		}
	}
}