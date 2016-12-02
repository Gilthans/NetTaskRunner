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
			// We have to sepeare the expressions and use ToList here to make sure 
			// we don't have tasks finishing while the loop is still running...
			var dependencyFreeTasks = _tasksWrappers.Values.Where(task => task.UnmetDependencies == 0).ToList();

			var tasksToWaitFor = dependencyFreeTasks
										.Select(taskToPerform => Task.Run(() => PerformTask(taskToPerform)))
										.ToList();

			return Task.WhenAll(tasksToWaitFor).ContinueWith(ResetTaskWrappers);
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

		private void ResetTaskWrappers(Task _)
		{
			foreach (var task in _tasksWrappers.Values)
				UpdateTaskDependencies(task);
		}

		private void PerformTask(TaskWrapper task)
		{
			task.ActualTask.Perform();

			var tasksToWaitFor = new List<Task>();
			foreach (var dependantTask in task.DependantTasks)
			{
				lock (dependantTask)
				{
					dependantTask.UnmetDependencies--;
					if (dependantTask.UnmetDependencies == 0)
					{
						var taskToPerform = dependantTask;
						tasksToWaitFor.Add(Task.Run(() => PerformTask(taskToPerform)));
					}
				}
			}

			Task.WhenAll(tasksToWaitFor).Wait();
		}

		private void PrepareTasksWrappers()
		{
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
