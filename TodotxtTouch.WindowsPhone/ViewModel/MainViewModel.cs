using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using EZLibrary;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Threading;
using Microsoft.Phone.Reactive;
using todotxtlib.net;
using TodotxtTouch.WindowsPhone.Messages;
using TodotxtTouch.WindowsPhone.Service;
using TodotxtTouch.WindowsPhone.Tasks;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
	public class MainViewModel : ViewModelBase
	{
	    private readonly ApplicationSettings _applicationSettings;

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

		public const string SelectedProjectPropertyName = "SelectedProject";
		public const string SelectedContextPropertyName = "SelectedContext";

		public const string ApplicationTitlePropertyName = "ApplicationTitle";

		/// <summary>
		/// The <see cref="Filters" /> property's name.
		/// </summary>
		public const string FiltersPropertyName = "Filters";

		#endregion

		#region Backing fields

		private TaskFileService _archiveFileService;
		private List<TaskFilter> _filters = new List<TaskFilter>();
		private TaskLoadingState _loadingState = TaskLoadingState.Ready;
		private IObservable<IEvent<LoadingStateChangedEventArgs>> _loadingStateObserver;
		private string _selectedContext;
		private string _selectedProject;
		private Task _selectedTask;
		private Task _selectedTaskDraft;
		private TaskFileService _taskFileService;
		private IObservable<IEvent<TaskListChangedEventArgs>> _taskListChangedObserver;

		#endregion

		/// <summary>
		/// The <see cref="Busy" /> property's name.
		/// </summary>
		public const string BusyPropertyName = "Busy";

		private readonly List<Task> _selectedTasks = new List<Task>();
		private bool _busy;

		#region Commands

		private bool _workingWithSelectedTasks;

		public RelayCommand RemoveSelectedTasksCommand { get; private set; }

		public RelayCommand ViewTaskDetailsCommand { get; private set; }

		public RelayCommand InitiateCallCommand { get; private set; }

		public RelayCommand SaveCurrentTaskCommand { get; private set; }

		public RelayCommand AddTaskCommand { get; private set; }

		public RelayCommand FilterByContextCommand { get; private set; }

		public RelayCommand FilterByProjectCommand { get; private set; }

		public RelayCommand ArchiveTasksCommand { get; private set; }

		public RelayCommand MarkSelectedTasksCompleteCommand { get; private set; }

		public RelayCommand<SelectionChangedEventArgs> SelectionChangedCommand { get; private set; }

		public RelayCommand SyncCommand { get; private set; }

        public RelayCommand StartupSyncCommand { get; private set; }

		private bool CanViewTaskDetailsExecute()
		{
			var canExecute = TaskFileServiceReady && SelectedTask != null;

			return canExecute;
		}

		private bool CanInitiateCallExecute()
		{
			var canExecute = TaskFileServiceReady && SelectedTask != null;

			return canExecute;
		}

		private void WireUpCommands()
		{
			ViewTaskDetailsCommand = new RelayCommand(ViewTask, CanViewTaskDetailsExecute);

			InitiateCallCommand = new RelayCommand(InitiateCall, CanInitiateCallExecute);

			AddTaskCommand = new RelayCommand(AddTask, () => TaskFileServiceReady);

			SaveCurrentTaskCommand = new RelayCommand(SaveCurrentTask,
			                                          () => TaskFileServiceReady
			                                                && SelectedTaskDraft != null);

			FilterByContextCommand = new RelayCommand(FilterByContext, () =>
			                                                           TaskFileServiceReady);

			FilterByProjectCommand = new RelayCommand(FilterByProject, () => TaskFileServiceReady);

			ArchiveTasksCommand = new RelayCommand(InitiateArchiveTasks,
			                                       () => TaskFileServiceReady);

			SelectionChangedCommand = new RelayCommand<SelectionChangedEventArgs>(args =>
				{
					if (args != null)
					{
						foreach (var task in args.AddedItems)
						{
							_selectedTasks.Add(task as Task);
						}

						foreach (var task in args.RemovedItems)
						{
							_selectedTasks.Remove(task as Task);
						}
					}
				}, args => !_workingWithSelectedTasks);

			MarkSelectedTasksCompleteCommand = new RelayCommand(MarkSelectedTasksComplete, () => TaskFileServiceReady);

			RemoveSelectedTasksCommand = new RelayCommand(RemoveSelectedTasks, () => TaskFileServiceReady);

			SyncCommand = new RelayCommand(Sync, () => TaskFileServiceReady);

            StartupSyncCommand = new RelayCommand(Sync, () => TaskFileServiceReady && _applicationSettings.SyncOnStartup);
		}

		private void Sync()
		{
			_taskFileService.Sync();
		}

		private void RemoveSelectedTasks()
		{
            if (_selectedTasks.Count == 0)
            {
                return;
            }

			_workingWithSelectedTasks = true;

			foreach (var task in _selectedTasks)
			{
				if (task != null)
				{
					TaskList.Delete(task);
				}
			}

            _selectedTasks.Clear();

            _taskFileService.SaveTasks();

		    _workingWithSelectedTasks = false;
		}

		private void MarkSelectedTasksComplete()
		{
            if (_selectedTasks.Count == 0)
            {
                return;
            }

			_workingWithSelectedTasks = true;

			foreach (var task in _selectedTasks)
			{
				if (task != null)
				{
					task.Completed = true;
				}
			}

            _taskFileService.SaveTasks();
            
			_workingWithSelectedTasks = false;
		}

		private void InitiateArchiveTasks()
		{
		    _archiveFileService.LoadingStateChanged += ArchiveTasks;

            _taskFileService.LoadingState = TaskLoadingState.Syncing;

            _archiveFileService.Sync();
		}

        private void ArchiveTasks(object obj, LoadingStateChangedEventArgs args)
        {
	        if (args.LoadingState != TaskLoadingState.Ready)
	        {
		        return;
	        }

	        _archiveFileService.LoadingStateChanged -= ArchiveTasks;

	        // TODO Have setting for preserving line numbers
	        var completedTasks = _taskFileService.TaskList.RemoveCompletedTasks(false);

	        foreach (var completedTask in completedTasks)
	        {
		        _archiveFileService.TaskList.Add(completedTask);
	        }

	        _archiveFileService.SaveTasks();
	        _taskFileService.SaveTasks();

	        _archiveFileService.LoadingStateChanged += FinishSavingArchive;

	        _archiveFileService.Sync();
        }

        private void FinishSavingArchive(object obj, LoadingStateChangedEventArgs args)
        {
            if (args.LoadingState == TaskLoadingState.Ready)
            {
                _archiveFileService.LoadingStateChanged -= FinishSavingArchive;

                _taskFileService.Sync();
            }
        }

		private void FilterByContext()
		{
			var context = SelectedContext;

			if (!string.IsNullOrEmpty(context))
			{
				Filters.Add(new ContextTaskFilter(t => t.Contexts.Contains(context), context));

				Messenger.Default.Send<DrillDownMessage, MainPivot>(
					new DrillDownMessage(TaskFilterFactory.CreateFilterString(Filters)));
			}
		}

		private void FilterByProject()
		{
			var project = SelectedProject;

			if (!string.IsNullOrEmpty(project))
			{
				Filters.Add(new ProjectTaskFilter(t => t.Projects.Contains(project), project));

				Messenger.Default.Send<DrillDownMessage, MainPivot>(
					new DrillDownMessage(TaskFilterFactory.CreateFilterString(Filters)));
			}
		}

		private void AddTask()
		{
			SelectedTask = null;

			SelectedTaskDraft = new Task(string.Empty, null, null, Filters.CreateDefaultBodyText());

		    SelectedTaskDraftIsNew = true;

			Messenger.Default.Send(new ViewTaskMessage());
		}

		private void ViewTask()
		{
			SelectedTaskDraft = SelectedTask.Copy();

		    SelectedTaskDraftIsNew = false;

			Messenger.Default.Send(new ViewTaskMessage());
		}

		private void InitiateCall()
		{
			Messenger.Default.Send(new InitiateCallMessage());
		}

		private void SaveCurrentTask()
		{
			if (SelectedTask == null)
			{
				_taskFileService.TaskList.Add(SelectedTaskDraft);
			}
			else
			{
				_taskFileService.UpdateTask(SelectedTaskDraft, SelectedTask);
			}

            _taskFileService.SaveTasks();
		}

		#endregion

		/// <summary>
		/// Initializes a new instance of the MainViewModel class.
		/// </summary>
		public MainViewModel(PrimaryTaskFileService taskFileService, ArchiveTaskFileService archiveFileService, ApplicationSettings applicationSettings)
		{
		    _applicationSettings = applicationSettings;
			Priorities = new List<string> {string.Empty};
			Observable.Range(65, 26).Select(n => ((char) n).ToString()).Subscribe(p => Priorities.Add(p));

		    if (IsInDesignMode)
			{
				// Code runs in Blend --> create design time data.
				TaskList = new TaskList
				{
					new Task("A", null, null,
						"This is a designer task that might be really long the quick brown fox jumped over the lazy dogs"),
					new Task("", null, null, "This is a designer task2"), new Task("", null, null,
						"This is a designer task3 Lorem ipsum dolor sit amet, consectetur adipisicing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.")
				};

				var b = new Task("B", null, null, "This is a designer task4");
				b.ToggleCompleted();
				TaskList.Add(b);
				TaskList.Add(new Task("C", null, null, "This is a designer task5"));

				TaskList.Add(new Task("This task has two contexts @home @work"));
				TaskList.Add(new Task("This task has two projects +planvacation +fixstove"));
				TaskList.Add(new Task("This task has one of each @home +fixstove"));
                TaskList.Add(new Task("")); // Blank task line

				_selectedTask = TaskList[3];
			}
			else
			{
				// Code runs "for real"
				WireupTaskFileServices(taskFileService, archiveFileService);

				Messenger.Default.Register<DrillDownMessage>(this, Filter);
			    Messenger.Default.Register<ApplicationStartedMessage>(this, message =>
			        {
                        LocalHasChanges = _taskFileService.LocalHasChanges;

			            if(StartupSyncCommand.CanExecute(null))
			            {
			                StartupSyncCommand.Execute(null);
			            }
			        });

				WireUpCommands();

			    
			}
		}

	    public IEnumerable<string> Projects
		{
			get
			{
				return TaskList.SelectMany(task => task.Projects,
				                           (task, project) => project).Distinct().OrderBy(project => project);
			}
		}

		public IEnumerable<string> Contexts
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

				var oldValue = _loadingState;
				_loadingState = value;

				// Update bindings and broadcast change using GalaSoft.MvvmLight.Messenging
				RaisePropertyChanged(LoadingStatePropertyName, oldValue, value, true);

				Busy = _loadingState != TaskLoadingState.Ready;

				switch(_loadingState)
				{
					case TaskLoadingState.Syncing:
						BusyDoingWhat = "Synchronizing";
						break;
					case TaskLoadingState.Saving:
						BusyDoingWhat = "Saving";
						break;
					case TaskLoadingState.Loading:
						BusyDoingWhat = "Loading";
						break;
					case TaskLoadingState.Ready:
						BusyDoingWhat = string.Empty;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		public List<string> Priorities { get; }

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

	    public bool SelectedTaskDraftIsNew { get; set; }

	    /// <summary>
		/// Gets the SelectedContext property.
		/// Changes to that property's value raise the PropertyChanged event. 
		/// </summary>
		public string SelectedContext
		{
			get { return _selectedContext; }

			set
			{
				if (_selectedContext == value)
				{
					return;
				}

				_selectedContext = value;

				// Update bindings, no broadcast
				RaisePropertyChanged(SelectedContextPropertyName);
			}
		}

		/// <summary>
		/// Gets the SelectedProject property.
		/// Changes to that property's value raise the PropertyChanged event. 
		/// </summary>
		public string SelectedProject
		{
			get { return _selectedProject; }

			set
			{
				if (_selectedProject == value)
				{
					return;
				}

				_selectedProject = value;

				// Update bindings, no broadcast
				RaisePropertyChanged(SelectedProjectPropertyName);
			}
		}

		/// <summary>
		/// Gets the ApplicationTitle property.
		/// </summary>
		public string ApplicationTitle
		{
			get
			{
				var filters = Filters.ToCommaDelimitedList(f => f.Description);

				return "Todo.txt" +
				       (filters.Length > 0 ? " - " : string.Empty) + filters;
			}
		}

		/// <summary>
		/// Gets the TaskList property.
		/// Changes to that property's value raise the PropertyChanged event. 
		/// </summary>
		private TaskList TaskList { get; set; }

		public IEnumerable<Task> AllTasks => TaskList.AsEnumerable().ApplyFilters(Filters).ApplySorts();

		public IEnumerable<Task> CompletedTasks
		{
			get { return TaskList.Where(t => t.Completed).ApplyFilters(Filters).ApplySorts(); }
		}

		/// <summary>
		/// Gets the Filters property.
		/// Changes to that property's value raise the PropertyChanged event for:
		/// AllTasks, CompletedTasks, ApplicationTitle, Contexts, Projects
		/// </summary>
		public List<TaskFilter> Filters
		{
			get { return _filters; }

			set
			{
				if (_filters == value)
				{
					return;
				}

				_filters = value;

				RaisePropertyChanged(AllTasksPropertyName);
				RaisePropertyChanged(CompletedTasksPropertyName);
				RaisePropertyChanged(ApplicationTitlePropertyName);
				RaisePropertyChanged(ContextsPropertyName);
				RaisePropertyChanged(ProjectsPropertyName);
			}
		}

		private bool TaskFileServiceReady => _taskFileService.LoadingState == TaskLoadingState.Ready;

		public bool LocalHasChanges
	    {
            get { return _localHasChanges; }

            set
            {
                if (_localHasChanges == value)
                {
                    return;
                }

                _localHasChanges = value;

                DispatcherHelper.CheckBeginInvokeOnUI(() => RaisePropertyChanged(LocalHasChangesPropertyName));
            }
	    }

	    protected string LocalHasChangesPropertyName => "LocalHasChanges";

		/// <summary>
		/// Gets the Busy property.
		/// Changes to that property's value raise the PropertyChanged event. 
		/// </summary>
		public bool Busy
		{
			get { return _busy; }

			set
			{
				if (_busy == value)
				{
					return;
				}

				_busy = value;

				// Update bindings, no broadcast
				// On UI thread, so the "busy" indicator can do it's thing
				DispatcherHelper.CheckBeginInvokeOnUI(() => RaisePropertyChanged(BusyPropertyName));
			}
		}

		/// <summary>
		/// The <see cref="BusyDoingWhat" /> property's name.
		/// </summary>
		public const string BusyDoingWhatPropertyName = "BusyDoingWhat";

		private string _busyDoingWhat = string.Empty;
	    private bool _localHasChanges;

	    /// <summary>
		/// Gets the BusyDoingWhat property.
		/// Changes to that property's value raise the PropertyChanged event. 
		/// </summary>
		public string BusyDoingWhat
		{
			get { return _busyDoingWhat; }

			set
			{
				if (_busyDoingWhat == value)
				{
					return;
				}

				_busyDoingWhat = value;

				// Update bindings, no broadcast
				DispatcherHelper.CheckBeginInvokeOnUI(() => RaisePropertyChanged(BusyDoingWhatPropertyName));
			}
		}

		public void WireupTaskFileServices(PrimaryTaskFileService ptfs, ArchiveTaskFileService atfs)
		{
			_archiveFileService = atfs;

			_taskFileService = ptfs;

			TaskList = _taskFileService.TaskList;

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

			Observable.FromEvent<SynchronizationErrorEventArgs>(_taskFileService, "SynchronizationError")
				.Subscribe(e => Messenger.Default.Send(new SynchronizationErrorMessage(e.EventArgs.Exception)));

            _taskFileService.LocalHasChangesChanged += TaskFileServiceOnLocalHasChangesChanged;
		}

	    private void TaskFileServiceOnLocalHasChangesChanged(object sender, LocalHasChangesChangedEventArgs localHasChangesChangedEventArgs)
	    {
	        LocalHasChanges = localHasChangesChangedEventArgs.LocalHasChanges;
	    }

	    private void Filter(DrillDownMessage message)
		{
			Filters = TaskFilterFactory.ParseFilterString(message.Filter);
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

			if (!string.IsNullOrEmpty(state.SelectedTaskDraft))
			{
				SelectedTaskDraft = new Task(state.SelectedTaskDraft);
			}

			if (!string.IsNullOrEmpty(state.SelectedTask))
			{
				var selectedTask = TaskList.FirstOrDefault(t => t.ToString() == state.SelectedTask);
				if (selectedTask != null)
				{
					SelectedTask = selectedTask;
				}
			}
		}
	}
}