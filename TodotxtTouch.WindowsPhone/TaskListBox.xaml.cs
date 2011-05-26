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
			Loaded += new RoutedEventHandler(TaskListBox_Loaded);
		}

		void TaskListBox_Loaded(object sender, RoutedEventArgs e)
		{
			this.TaskList.Width = System.Windows.Application.Current.Host.Content.ActualWidth;
		}

		#region public IEnumerable ItemsSource

		/// <summary>
		/// Identifies the ItemsSource dependency property.
		/// </summary>
		public static DependencyProperty ItemsSourceProperty =
			DependencyProperty.Register("ItemsSource", typeof (IEnumerable), typeof (TaskListBox),
			                            new PropertyMetadata(new PropertyChangedCallback(ItemsSourceChanged)));

		public IEnumerable ItemsSource
		{
			get { return (IEnumerable) TaskList.GetValue(ItemsControl.ItemsSourceProperty); }
			set { TaskList.SetValue(ItemsControl.ItemsSourceProperty, value); }
		}

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

		#endregion public IEnumerable ItemsSource
	}
}