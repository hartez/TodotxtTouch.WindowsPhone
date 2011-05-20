using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Controls;
using EZLibrary;
using EZLibrary.Collections.Generic;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Phone.Reactive;
using todotxtlib.net;
using TodotxtTouch.WindowsPhone.Service;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
	/// <summary>
	/// This class contains properties that the main View can data bind to.
	/// <para>
	/// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
	/// </para>
	/// <para>
	/// You can also use Blend to data bind with the tool's support.
	/// </para>
	/// <para>
	/// See http://www.galasoft.ch/mvvm
	/// </para>
	/// </summary>
	public class MainViewModel : ViewModelBase
	{
		#region Property Names

		/// <summary>
		/// The <see cref="TaskList" /> property's name.
		/// </summary>
		public const string AllTasksPropertyName = "AllTasks";

		public const string CompletedTasksPropertyName = "CompletedTasks";

		/// <summary>
		/// The <see cref="LoadingState" /> property's name.
		/// </summary>
		public const string LoadingStatePropertyName = "LoadingState";

		/// <summary>
		/// The <see cref="SelectedTask" /> property's name.
		/// </summary>
		public const string SelectedTaskPropertyName = "SelectedTask";

		/// <summary>
		/// The <see cref="SelectedTaskDraft" /> property's name.
		/// </summary>
		public const string SelectedTaskDraftPropertyName = "SelectedTaskDraft";

		public const string ContextsPropertyName = "Contexts";
		public const string ProjectsPropertyName = "Projects";

		public const string ApplicationTitlePropertyName = "ApplicationTitle";

		#endregion

		#region Backing fields

		private readonly ObservableCollection<string> _availablePriorities = new ObservableCollection<string>();
		private readonly TaskFileService _taskFileService;
		private TaskLoadingState _loadingState = TaskLoadingState.NotLoaded;
		private Task _selectedTask;
		private Task _selectedTaskDraft;

		#endregion

		private readonly ObservableStack<TaskFilter> _filters = new ObservableStack<TaskFilter>();
		private readonly IObservable<IEvent<LoadingStateChangedEventArgs>> _loadingStateObserver;
		private readonly IObservable<IEvent<TaskListChangedEventArgs>> _taskListChangedObserver;
		private IObservable<IEvent<NotifyCollectionChangedEventArgs>> _filterObserver;

		/// <summary>
		/// Initializes a new instance of the MainViewModel class.
		/// </summary>
		public MainViewModel(TaskFileService taskFileService)
		{
			_taskFileService = taskFileService;

			_loadingStateObserver = Observable.FromEvent<LoadingStateChangedEventArgs>(
				_taskFileService, "LoadingStateChanged");

			_loadingStateObserver.Subscribe(e => LoadingState = e.EventArgs.LoadingState);

			_taskListChangedObserver = Observable.FromEvent<TaskListChangedEventArgs>(
				_taskFileService, "TaskListChanged");

			_taskListChangedObserver.Subscribe(e =>
				{
					RaisePropertyChanged(AllTasksPropertyName);
					RaisePropertyChanged(CompletedTasksPropertyName);
					RaisePropertyChanged(ContextsPropertyName);
					RaisePropertyChanged(ProjectsPropertyName);
				});


			_filterObserver = Observable.FromEvent<NotifyCollectionChangedEventArgs>(_filters, "CollectionChanged");

			_filterObserver.Subscribe(e =>
				{
					RaisePropertyChanged(AllTasksPropertyName);
					RaisePropertyChanged(CompletedTasksPropertyName);
					RaisePropertyChanged(ApplicationTitlePropertyName);
				});

			Messenger.Default.Register<DrillUpMessage>(this, message => Filters.Pop());

			if (IsInDesignMode)
			{
				// Code runs in Blend --> create design time data.
				Observable.Range(65, 26).Select(n => ((char) n).ToString()).Subscribe(p => _availablePriorities.Add(p));

				TaskList.Add(new Task("A", null, null, "This is a designer task"));
				TaskList.Add(new Task("", null, null, "This is a designer task2"));
				TaskList.Add(new Task("", null, null, "This is a designer task3"));
				var b = new Task("B", null, null, "This is a designer task4");
				b.ToggleCompleted();
				TaskList.Add(b);
				TaskList.Add(new Task("C", null, null, "This is a designer task5"));

				_selectedTask = TaskList[3];
			}
			else
			{
				// Code runs "for real"
				WireUpCommands();
			}
		}

		public IEnumerable<String> Projects
		{
			get
			{
				return TaskList.SelectMany(task => task.Projects,
				                           (task, project) => project).Distinct().OrderBy(project => project);
			}
		}

		public IEnumerable<String> Contexts
		{
			get
			{
				return TaskList.SelectMany(task => task.Contexts,
				                           (task, context) => context).Distinct().OrderBy(context => context);
			}
		}

		/// <summary>
		/// Gets the LoadingState property.
		/// This property's value is broadcasted by the Messenger's default instance when it changes.
		/// </summary>
		public TaskLoadingState LoadingState
		{
			get { return _loadingState; }

			private set
			{
				if (_loadingState == value)
				{
					return;
				}

				TaskLoadingState oldValue = _loadingState;
				_loadingState = value;

				// Update bindings and broadcast change using GalaSoft.MvvmLight.Messenging
				RaisePropertyChanged(LoadingStatePropertyName, oldValue, value, true);
			}
		}

		public ObservableCollection<String> AvailablePriorities
		{
			get { return _availablePriorities; }
		}

		/// <summary>
		/// Gets the SelectedTask property.
		/// Changes to that property's value raise the PropertyChanged event. 
		/// </summary>
		public Task SelectedTask
		{
			get { return _selectedTask; }

			set
			{
				if (_selectedTask == value)
				{
					return;
				}

				_selectedTask = value;

				// Update bindings, no broadcast
				RaisePropertyChanged(SelectedTaskPropertyName);
			}
		}

		/// <summary>
		/// Gets the SelectedTaskDraft property.
		/// Changes to that property's value raise the PropertyChanged event. 
		/// </summary>
		public Task SelectedTaskDraft
		{
			get { return _selectedTaskDraft; }

			set
			{
				if (_selectedTaskDraft == value)
				{
					return;
				}

				_selectedTaskDraft = value;

				// Update bindings, no broadcast
				RaisePropertyChanged(SelectedTaskDraftPropertyName);
			}
		}

		/// <summary>
		/// Gets the ApplicationTitle property.
		/// </summary>
		public string ApplicationTitle
		{
			get
			{
				string filters = Filters.ToCommaDelimitedList(f => f.Description);

				return "Todo.txt" +
				       (filters.Length > 0 ? " - " : String.Empty) + filters;
			}
		}

		/// <summary>
		/// Gets the TaskList property.
		/// Changes to that property's value raise the PropertyChanged event. 
		/// </summary>
		private TaskList TaskList
		{
			get { return _taskFileService.TaskList; }
		}

		public IEnumerable<Task> AllTasks
		{
			get { return TaskList.AsEnumerable().ApplyFilters(Filters); }
		}

		public IEnumerable<Task> CompletedTasks
		{
			get { return TaskList.Where(t => t.Completed).ApplyFilters(Filters); }
		}

		private ObservableStack<TaskFilter> Filters
		{
			get { return _filters; }
		}

		#region Commands

		public RelayCommand ViewTaskDetailsCommand { get; private set; }

		public RelayCommand SaveCurrentTaskCommand { get; private set; }

		public RelayCommand RevertCurrentTaskCommand { get; private set; }

		public RelayCommand AddTaskCommand { get; private set; }

		public RelayCommand<SelectionChangedEventArgs> FilterByContextCommand { get; private set; }

		private void WireUpCommands()
		{
			ViewTaskDetailsCommand = new RelayCommand(ViewTask, () =>
			                                                    _taskFileService.LoadingState == TaskLoadingState.Ready
			                                                    && SelectedTask != null);

			AddTaskCommand = new RelayCommand(AddTask, () => _taskFileService.LoadingState == TaskLoadingState.Ready);

			SaveCurrentTaskCommand = new RelayCommand(SaveCurrentTask,
			                                          () => _taskFileService.LoadingState == TaskLoadingState.Ready
			                                                && SelectedTaskDraft != null);

			FilterByContextCommand = new RelayCommand<SelectionChangedEventArgs>(FilterByContext,
			                                                                     e =>
			                                                                     _taskFileService.LoadingState ==
			                                                                     TaskLoadingState.Ready);

			RevertCurrentTaskCommand = new RelayCommand(RevertCurrentTask,
				() => _taskFileService.LoadingState == TaskLoadingState.Ready
															&& SelectedTaskDraft != null);
		}

		private void RevertCurrentTask()
		{
			if (SelectedTask == null)
			{
				SelectedTaskDraft.Empty();
			}
			else
			{
				SelectedTaskDraft = SelectedTask.Copy();
			}
		}

		private void FilterByContext(SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count > 0)
			{
				string context = e.AddedItems[0].ToString();

				Filters.Push(new TaskFilter(t => t.Contexts.Contains(context), string.Format("Context = {0}", context)));

				Messenger.Default.Send(new DrillDownMessage(Filters.Count.ToString()));
			}
		}

		private void AddTask()
		{
			SelectedTask = null;

			SelectedTaskDraft = new Task(String.Empty, null, null, String.Empty);

			UpdateAvailablePriorities();

			Messenger.Default.Send(new ViewTaskMessage());
		}

		private void ViewTask()
		{
			SelectedTaskDraft = SelectedTask;

			UpdateAvailablePriorities();

			Messenger.Default.Send(new ViewTaskMessage());
		}

		private void SaveCurrentTask()
		{
			if (SelectedTask == null)
			{
				_taskFileService.AddTask(SelectedTaskDraft);
			}
			else
			{
				_taskFileService.UpdateTask(SelectedTaskDraft, SelectedTask);
			}
		}

		#endregion

		private void UpdateAvailablePriorities()
		{
			_availablePriorities.Clear();
			_availablePriorities.Add("");

			IEnumerable<String> prioritiesInUse =
				(from t in TaskList
				 where t.IsPriority && t.Priority != SelectedTaskDraft.Priority
				 orderby t.Priority
				 select t.Priority).Distinct();

			// Generate the possible priorities, then skip over the ones that are already in use
			Observable.Range(65, 26).Select(n => ((char) n).ToString()).SkipWhile(c => prioritiesInUse.Contains(c))
				.Subscribe(priority => _availablePriorities.Add(priority));
		}

		public void SetState(TombstoneState state)
		{
			if (LoadingState != TaskLoadingState.Ready)
			{
				// If we're not in a state where we can safely load the state,
				// wait until we are and then run this method again.
				_loadingStateObserver
					.Where(e => e.EventArgs.LoadingState == TaskLoadingState.Ready)
					.Take(1)
					.Subscribe(e => SetState(state));

				return;
			}

			if (!String.IsNullOrEmpty(state.SelectedTaskDraft))
			{
				SelectedTaskDraft = new Task(state.SelectedTaskDraft);
			}

			if (!String.IsNullOrEmpty(state.SelectedTask))
			{
				Task selectedTask = TaskList.FirstOrDefault(t => t.ToString() == state.SelectedTask);
				if (selectedTask != null)
				{
					SelectedTask = selectedTask;
				}
			}
		}
	}
}