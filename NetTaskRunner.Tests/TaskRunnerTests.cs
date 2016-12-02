using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTaskRunner;

namespace TaskRun4Net.Tests
{
	[TestClass]
	public class TaskRunnerTests
	{
		[TestMethod]
		public void RunAllTasks_ShouldRunSingleRegisteredTask()
		{
			// Arrange
			var runner = new TaskRunner();
			var task = new ControlledTask("Task1", true);
			runner.RegisterTask(task);

			// Act
			runner.RunAllTasks();

			// Assert
			Assert.IsTrue(task.IsPerformed);
		}

		[TestMethod]
		public void RunAllTasks_ShouldWaitForDependencyToRunTask()
		{
			// Arrange
			var runner = new TaskRunner();
			var controlledTask = new ControlledTask("Task1");
			var dependantTask = new ControlledTask("Task2", new[] { "Task1" }, true);
			runner.RegisterTask(controlledTask);
			runner.RegisterTask(dependantTask);

			// Act
			runner.RunAllTasks();

			// Assert
			Assert.IsTrue(controlledTask.IsPerformed);
			Assert.IsFalse(dependantTask.IsPerformed);
			controlledTask.FinishPerforming();
			Assert.IsTrue(dependantTask.IsPerformed);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void RegisterTask_ShouldThrowExceptionForNonExistingDependency()
		{
			// Arrange
			var runner = new TaskRunner();
			var dependantTask = new ControlledTask("Task2", new[] { "Task1" });
			runner.RegisterTask(dependantTask);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void RegisterTask_ShouldThrowExceptionForTasksWithSameName()
		{
			// Arrange
			var runner = new TaskRunner();
			var task1 = new ControlledTask("Task");
			var task2 = new ControlledTask("Task");
			runner.RegisterTask(task1);
			runner.RegisterTask(task2);
		}

		[TestMethod]
		public void RunAllTasks_ShouldRunAllPossibleTasksAtAGivenTime()
		{
			// Arrange
			var runner = new TaskRunner();
			var task1 = new ControlledTask("Task1");
			var task2 = new ControlledTask("Task2");
			var dependantTask = new ControlledTask("Task3", new[] { "Task2" }, true);
			runner.RegisterTask(task1);
			runner.RegisterTask(task2);
			runner.RegisterTask(dependantTask);

			// Act
			runner.RunAllTasks();

			// Assert
			Assert.IsTrue(task1.IsPerformed);
			Assert.IsTrue(task2.IsPerformed);
			Assert.IsFalse(dependantTask.IsPerformed);

			task2.FinishPerforming();
			Assert.IsTrue(dependantTask.IsPerformed);

			task1.FinishPerforming();
		}

		[TestMethod]
		public void RunAllTasks_ShouldWaitForAllDependencies()
		{
			// Arrange
			var runner = new TaskRunner();
			var task1 = new ControlledTask("Task1");
			var task2 = new ControlledTask("Task2");
			var dependantTask = new ControlledTask("Task3", new[] { "Task1", "Task2" }, true);
			runner.RegisterTask(task1);
			runner.RegisterTask(task2);
			runner.RegisterTask(dependantTask);

			// Act
			runner.RunAllTasks();

			// Assert
			task2.FinishPerforming();
			Assert.IsFalse(dependantTask.IsPerformed);

			task1.FinishPerforming();
			Assert.IsTrue(dependantTask.IsPerformed);
		}

		[TestMethod]
		public void RunAllTasks_ShouldOnlyFinishTaskWhenAllTasksAreDone()
		{
			// Arrange
			var runner = new TaskRunner();
			var task1 = new ControlledTask("Task1");
			var task2 = new ControlledTask("Task2");
			var dependantTask = new ControlledTask("Task3", new[] { "Task1", "Task2" });
			var dependantTask2 = new ControlledTask("Task4", new[] { "Task3", "Task2" });
			runner.RegisterTask(task1);
			runner.RegisterTask(task2);
			runner.RegisterTask(dependantTask);
			runner.RegisterTask(dependantTask2);

			// Act
			var finishedTask = runner.RunAllTasks();

			// Assert
			task1.FinishPerforming();
			task2.FinishPerforming();
			dependantTask.FinishPerforming();
			Assert.IsFalse(finishedTask.Wait(100));

			dependantTask2.FinishPerforming();
			Assert.IsTrue(finishedTask.Wait(100));
		}

		[TestMethod]
		public void RunAllTasks_TaskWithDoubleDependencyShouldWork()
		{
			// Arrange
			var runner = new TaskRunner();
			var task1 = new ControlledTask("Task1", true);
			var dependantTask = new ControlledTask("Task2", new[] { "Task1", "Task1" }, true);
			runner.RegisterTask(task1);
			runner.RegisterTask(dependantTask);

			// Act
			runner.RunAllTasks();

			// Assert
			Assert.IsTrue(dependantTask.IsPerformed);
		}

		[TestMethod]
		public void RunAllTasks_RunningATaskWithManyDependenciesShouldNotCauseRace()
		{
			// Arrange
			var runner = new TaskRunner();
			var dependencies = new List<string>();
			for (int i = 0; i < 100; i++)
			{
				var task = new ControlledTask(i.ToString(), true);
				runner.RegisterTask(task);
				dependencies.Add(task.Name);
			}
			var dependantTask = new ControlledTask("Task", dependencies, true);
			runner.RegisterTask(dependantTask);

			// Act
			runner.RunAllTasks();

			// Assert
			Assert.IsTrue(dependantTask.IsPerformed);
		}

		[TestMethod]
		public void Integration_ShouldOnlyFinishTaskWhenAllTasksAreDone()
		{
			// Arrange
			var random = new Random();
			var runner = new TaskRunner();
			var allTasks = new List<ControlledTask>();
			for (int i = 0; i < 100; i++)
			{
				var controlledTask = new ControlledTask(i.ToString(), true);
				allTasks.Add(controlledTask);
				runner.RegisterTask(controlledTask);
			}

			var existingTaskCount = 100;
			for (int i = 100; i < 1000; i++)
			{
				var dependencies = new List<string>();
				var dependencyCount = random.Next(10, 100);
				for (int j = 0; j < dependencyCount; j++)
					dependencies.Add(random.Next(0, existingTaskCount - 1).ToString());

				var controlledTask = new ControlledTask(i.ToString(), dependencies, true);
				runner.RegisterTask(controlledTask);
				allTasks.Add(controlledTask);

				existingTaskCount++;
			}

			// Act
			var isDone = runner.RunAllTasks().Wait(4000);

			// Assert
			Assert.IsTrue(isDone);
			foreach (var task in allTasks)
			{
				Assert.IsTrue(task.IsPerformed);
				Assert.AreEqual(1, task.RunCounter, string.Format("Task {0} was run incorrect number of times!", task.Name));
			}
		}

		// Test for nulls
		// Integration test with lots of tasks running
		// Test running twice in a row

		private class ControlledTask : ITask
		{
			private int _runCounter = 0;
			private readonly Semaphore _canFinishSemaphore = new Semaphore(0, 1);
			private readonly Semaphore _isPerformedSemaphore = new Semaphore(0, 1);
			public string Name { get; private set; }

			public IEnumerable<string> Dependencies { get; private set; }

			public int RunCounter { get { return _runCounter; } }

			public bool IsPerformed
			{
				get { return _isPerformedSemaphore.WaitOne(100); }
			}

			public ControlledTask(string name, IEnumerable<string> dependencies = null, bool finishAutomatically = false)
			{
				Name = name;
				Dependencies = dependencies ?? new string[] { };
				if (finishAutomatically)
					_canFinishSemaphore.Release();
			}

			public ControlledTask(string name, bool finishAutomatically)
				: this(name, null, finishAutomatically)
			{
			}

			public void Perform()
			{
				_isPerformedSemaphore.Release();
				Interlocked.Increment(ref _runCounter);
				_canFinishSemaphore.WaitOne();
			}

			public void FinishPerforming()
			{
				_canFinishSemaphore.Release();
			}
		}
	}
}
