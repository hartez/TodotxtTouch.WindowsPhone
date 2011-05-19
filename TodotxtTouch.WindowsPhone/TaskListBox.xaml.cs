using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace TodotxtTouch.WindowsPhone
{
	public partial class TaskListBox : UserControl
	{
		public TaskListBox()
		{
			InitializeComponent();
		}

		#region public IEnumerable ItemsSource

		/// <summary>
		/// Identifies the ItemsSource dependency property.
		/// </summary>
		public static DependencyProperty ItemsSourceProperty =
			DependencyProperty.Register("ItemsSource", typeof (IEnumerable), typeof (TaskListBox), new PropertyMetadata(new PropertyChangedCallback(ItemsSourceChanged)));

		private static void ItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var taskListBox = (d as TaskListBox);

			if (taskListBox != null)
			{
				taskListBox.SetItemsSource(e.NewValue);
			}
		}


		public void SetItemsSource(Object newValue)
		{
			TaskList.SetValue(ItemsControl.ItemsSourceProperty, newValue);
		}

		/// <summary>
		/// 
		/// </summary>
		public IEnumerable ItemsSource

		{
			get { return (IEnumerable)TaskList.GetValue(ItemsControl.ItemsSourceProperty); }


			set { TaskList.SetValue(ItemsControl.ItemsSourceProperty, value); }
		}

		#endregion public IEnumerable ItemsSource
	}
}