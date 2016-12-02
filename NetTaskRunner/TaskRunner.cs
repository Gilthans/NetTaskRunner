using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetTaskRunner
{
	public class TaskRunner
	{
		#region Fields

		private readonly Dictionary<string, TaskWrapper> _tasksWrappers = new Dictionary<string, TaskWrapper>();
		private Barrier _finishingBarrier;

		#endregion

		#region C'tor

		public TaskRunner()
		{
		}

		#endregion

		#region Public Methods

		public void RegisterTask(ITask newTask)
		{
			var newTaskWrapper = new TaskWrapper(newTask);
			_tasksWrappers.Add(newTask.Name, newTaskWrapper);

			UpdateTaskDependencies(newTaskWrapper);
		}

		public Task RunAllTasks()
		{
			_finishingBarrier = new Barrier(_tasksWrappers.Count + 1);

			// We have to do this sepearately use ToList to make sure 
			// we don't have tasks finishing while the loop is still running...
			var dependencyFreeTasks = _tasksWrappers.Values.Where(task => task.UnmetDependencies == 0).ToList();

			foreach (var task in dependencyFreeTasks)
				Task.Run(() => PerformTask(task));

			return Task.Run(() =>
			{
				_finishingBarrier.SignalAndWait();
				ResetTaskWrappers();
			});
		}

		#endregion

		#region Private Methods

		private void UpdateTaskDependencies(TaskWrapper task)
		{
			foreach (var dependency in task.ActualTask.Dependencies)
			{
				TaskWrapper dependencyWrapper;
				if (!_tasksWrappers.TryGetValue(dependency, out dependencyWrapper))
					throw new ArgumentException(string.Format("Task {0} depends on newTask {1} which was never registered!", task.ActualTask.Name,
						dependency));

				dependencyWrapper.DependantTasks.Add(task);
				task.UnmetDependencies++;
			}
		}

		private void ResetTaskWrappers()
		{
			foreach (var task in _tasksWrappers.Values)
			{
				task.DependantTasks.Clear();
				task.UnmetDependencies = 0;
			}

			foreach (var task in _tasksWrappers.Values)
				UpdateTaskDependencies(task);
		}

		private void PerformTask(TaskWrapper task)
		{
			task.ActualTask.Perform();

			var tasksToWaitFor = new List<Task>();
			foreach (var dependantTask in task.DependantTasks)
			{
				bool shouldPerform = false;
				lock (dependantTask)
				{
					dependantTask.UnmetDependencies--;
					if (dependantTask.UnmetDependencies == 0)
						shouldPerform = true;
				}

				if (shouldPerform)
				{
					var taskToPerform = dependantTask;
					tasksToWaitFor.Add(Task.Run(() => PerformTask(taskToPerform)));
				}
			}

			_finishingBarrier.RemoveParticipant();
		}

		private class TaskWrapper
		{
			public TaskWrapper(ITask task)
			{
				ActualTask = task;
				UnmetDependencies = 0;
				WasPerformed = false;
				DependantTasks = new List<TaskWrapper>();
			}

			public ITask ActualTask { get; private set; }

			public int UnmetDependencies { get; set; }

			public bool WasPerformed { get; set; }

			public IList<TaskWrapper> DependantTasks { get; private set; }
		}

		#endregion
	}
}
