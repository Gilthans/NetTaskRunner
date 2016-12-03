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
			if (newTask == null)
				throw new ArgumentNullException(nameof(newTask));
			var newTaskWrapper = new TaskWrapper(newTask);
			_tasksWrappers.Add(newTask.Name, newTaskWrapper);

			UpdateTaskDependencies(newTaskWrapper);
		}

		public Task<IArgumentHolder> RunAllTasks()
		{
			var finishingBarrier = new Barrier(_tasksWrappers.Count + 1);
			IArgumentHolder globalArgumentHolder = new ArgumentHolder();

			// We have to do this sepearately use ToList to make sure 
			// we don't have tasks finishing while the loop is still running...
			var dependencyFreeTasks = _tasksWrappers.Values.Where(task => task.UnmetDependencies == 0).ToList();

			foreach (var task in dependencyFreeTasks)
				Task.Run(() => PerformTask(task, globalArgumentHolder, finishingBarrier));

			return Task.Run(() =>
			{
				finishingBarrier.SignalAndWait();
				ResetTaskWrappers();
				return globalArgumentHolder;
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
				task.UnmetDependencies = 0;
				task.ArgumentHolder.Clear();
			}

			foreach (var task in _tasksWrappers.Values)
				foreach (var dependantTask in task.DependantTasks)
					dependantTask.UnmetDependencies++;
		}

		private void PerformTask(TaskWrapper task, IArgumentHolder globalArgumentHolder, Barrier finishingBarrier)
		{
			var result = task.ActualTask.Perform(task.ArgumentHolder);
			globalArgumentHolder.RegisterResult(task.ActualTask.Name, result);

			foreach (var dependantTask in task.DependantTasks)
			{
				bool shouldPerform = false;
				lock (dependantTask)
				{
					dependantTask.ArgumentHolder.RegisterResult(task.ActualTask.Name, result);
					dependantTask.UnmetDependencies--;
					if (dependantTask.UnmetDependencies == 0)
						shouldPerform = true;
				}

				if (shouldPerform)
				{
					var taskToPerform = dependantTask;
					Task.Run(() => PerformTask(taskToPerform, globalArgumentHolder, finishingBarrier));
				}
			}

			finishingBarrier.RemoveParticipant();
		}

		private class TaskWrapper
		{
			public TaskWrapper(ITask task)
			{
				ActualTask = task;
				UnmetDependencies = 0;
				DependantTasks = new List<TaskWrapper>();
				ArgumentHolder = new ArgumentHolder();
			}

			public ArgumentHolder ArgumentHolder { get; }

			public ITask ActualTask { get; }

			public int UnmetDependencies { get; set; }

			public IList<TaskWrapper> DependantTasks { get; }

			public override string ToString()
			{
				return string.Format("{0} ({1} dependencies, {2} depenedent)", ActualTask.Name, UnmetDependencies, DependantTasks.Count);
			}
		}

		#endregion
	}
}
