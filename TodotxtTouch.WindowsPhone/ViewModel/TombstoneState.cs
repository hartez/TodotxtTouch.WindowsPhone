using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
	public class TombstoneState
	{
		public TombstoneState(string selectedTask, string selectedTaskDraft)
		{
			SelectedTask = selectedTask;
			SelectedTaskDraft = selectedTaskDraft;
		}

		public static string ToJson(TombstoneState tombstoneState)
		{
			var sb = new StringBuilder();
			var serializer = new JsonSerializer();
			serializer.Serialize(new JsonTextWriter(new StringWriter(sb)), tombstoneState);
			return sb.ToString();
		}

		public static TombstoneState FromJson(string state)
		{
			var serializer = new JsonSerializer();
			return serializer.Deserialize<TombstoneState>(new JsonTextReader(new StringReader(state)));
		}

		public string SelectedTask { get; set; }
		public string SelectedTaskDraft { get; set; }
	}
}