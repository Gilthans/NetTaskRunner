using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetTaskRunner;
using System.Linq;
using System.Threading.Tasks;

namespace NetTaskRunner.Tests
{
	[TestClass]
	public class MissionRunnerTests
	{
		#region RegisterMission

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void RegisterMission_ShouldThrowExceptionForNonExistingDependency()
		{
			// Arrange
			var runner = new MissionRunner();
			var dependantMission = new ControlledMission("Mission2", new[] { "Mission1" });

			// Act
			runner.RegisterMission(dependantMission);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void RegisterMission_ShouldThrowExceptionForMissionsWithSameName()
		{
			// Arrange
			var runner = new MissionRunner();
			var mission1 = new ControlledMission("Mission");
			var mission2 = new ControlledMission("Mission");
			runner.RegisterMission(mission1);

			// Act
			runner.RegisterMission(mission2);
		}

		[TestMethod]
		public void RegisterMission_RegisteringMissionWithInvalidDependencyShouldNotAddToRunner()
		{
			// Arrange
			var runner = new MissionRunner();
			var mission = new ControlledMission("Mission", new[] { "NoMission" }, true);

			// Act
			try
			{
				runner.RegisterMission(mission);
			}
			catch (ArgumentException) { }

			// Assert
			var isDone = runner.PerformAllMissions().Wait(100);
			Assert.IsTrue(isDone);
			Assert.AreEqual(0, mission.RunCounter);
		}

		#endregion

		#region RunAllMissions

		[TestMethod]
		public void RunAllMissions_WithoutMissionsShouldFinishImmediately()
		{
			// Arrange
			var runner = new MissionRunner();

			// Act
			var isDone = runner.PerformAllMissions().Wait(100);

			// Assert
			Assert.IsTrue(isDone);
		}

		[TestMethod]
		public async Task RunAllMissions_WithoutMissionsShouldReturnEmptyArgumentHolder()
		{
			// Arrange
			var runner = new MissionRunner();

			// Act
			var argumentHolder = await runner.PerformAllMissions();

			// Assert
			Assert.IsNotNull(argumentHolder);
			Assert.AreEqual(0, argumentHolder.Count);
		}

		[TestMethod]
		public void RunAllMissions_ShouldRunSingleRegisteredMission()
		{
			// Arrange
			var runner = new MissionRunner();
			var mission = new ControlledMission("Mission1", true);
			runner.RegisterMission(mission);

			// Act
			runner.PerformAllMissions();

			// Assert
			Assert.IsTrue(mission.IsPerformed);
		}

		[TestMethod]
		public void RunAllMissions_ShouldWaitForDependencyToRunMission()
		{
			// Arrange
			var runner = new MissionRunner();
			var controlledMission = new ControlledMission("Mission1");
			var dependantMission = new ControlledMission("Mission2", new[] { "Mission1" }, true);
			runner.RegisterMission(controlledMission);
			runner.RegisterMission(dependantMission);

			// Act
			runner.PerformAllMissions();

			// Assert
			Assert.IsTrue(controlledMission.IsPerformed);
			Assert.IsFalse(dependantMission.IsPerformed);
			controlledMission.FinishPerforming();
			Assert.IsTrue(dependantMission.IsPerformed);
		}

		[TestMethod]
		public void RunAllMissions_ShouldRunAllPossibleMissionsAtAGivenTime()
		{
			// Arrange
			var runner = new MissionRunner();
			var mission1 = new ControlledMission("Mission1");
			var mission2 = new ControlledMission("Mission2");
			var dependantMission = new ControlledMission("Mission3", new[] { "Mission2" }, true);
			runner.RegisterMission(mission1);
			runner.RegisterMission(mission2);
			runner.RegisterMission(dependantMission);

			// Act
			runner.PerformAllMissions();

			// Assert
			Assert.IsTrue(mission1.IsPerformed);
			Assert.IsTrue(mission2.IsPerformed);
			Assert.IsFalse(dependantMission.IsPerformed);

			mission2.FinishPerforming();
			Assert.IsTrue(dependantMission.IsPerformed);

			mission1.FinishPerforming();
		}

		[TestMethod]
		public void RunAllMissions_ShouldWaitForAllDependencies()
		{
			// Arrange
			var runner = new MissionRunner();
			var mission1 = new ControlledMission("Mission1");
			var mission2 = new ControlledMission("Mission2");
			var dependantMission = new ControlledMission("Mission3", new[] { "Mission1", "Mission2" }, true);
			runner.RegisterMission(mission1);
			runner.RegisterMission(mission2);
			runner.RegisterMission(dependantMission);

			// Act
			runner.PerformAllMissions();

			// Assert
			mission2.FinishPerforming();
			Assert.IsFalse(dependantMission.IsPerformed);

			mission1.FinishPerforming();
			Assert.IsTrue(dependantMission.IsPerformed);
		}

		[TestMethod]
		public void RunAllMissions_ShouldOnlyFinishMissionWhenAllMissionsAreDone()
		{
			// Arrange
			var runner = new MissionRunner();
			var mission1 = new ControlledMission("Mission1");
			var mission2 = new ControlledMission("Mission2");
			var dependantMission = new ControlledMission("Mission3", new[] { "Mission1", "Mission2" });
			var dependantMission2 = new ControlledMission("Mission4", new[] { "Mission3", "Mission2" });
			runner.RegisterMission(mission1);
			runner.RegisterMission(mission2);
			runner.RegisterMission(dependantMission);
			runner.RegisterMission(dependantMission2);

			// Act
			var finishedMission = runner.PerformAllMissions();

			// Assert
			mission1.FinishPerforming();
			mission2.FinishPerforming();
			dependantMission.FinishPerforming();
			Assert.IsFalse(finishedMission.Wait(100));

			dependantMission2.FinishPerforming();
			Assert.IsTrue(finishedMission.Wait(100));
		}

		[TestMethod]
		public void RunAllMissions_MissionWithDoubleDependencyShouldWork()
		{
			// Arrange
			var runner = new MissionRunner();
			var mission1 = new ControlledMission("Mission1", true);
			var dependantMission = new ControlledMission("Mission2", new[] { "Mission1", "Mission1" }, true);
			runner.RegisterMission(mission1);
			runner.RegisterMission(dependantMission);

			// Act
			runner.PerformAllMissions();

			// Assert
			Assert.IsTrue(dependantMission.IsPerformed);
		}

		[TestMethod]
		public void RunAllMissions_RunningAMissionWithManyDependenciesShouldNotCauseRace()
		{
			// Arrange
			var runner = new MissionRunner();
			var dependencies = new List<string>();
			for (int i = 0; i < 100; i++)
			{
				var mission = new ControlledMission(i.ToString(), true);
				runner.RegisterMission(mission);
				dependencies.Add(mission.Name);
			}
			var dependantMission = new ControlledMission("Mission", dependencies, true);
			runner.RegisterMission(dependantMission);

			// Act
			runner.PerformAllMissions();

			// Assert
			Assert.IsTrue(dependantMission.IsPerformed);
		}

		[TestMethod]
		public void RunAllMissions_RunningTwiceShouldRunAllMissionsTwiceInTheRightOrder()
		{
			// Arrange
			var runner = new MissionRunner();
			var mission1 = new ControlledMission("Mission1");
			var mission2 = new ControlledMission("Mission2");
			var dependantMission = new ControlledMission("Mission3", new[] { "Mission1", "Mission2" });
			var dependantMission2 = new ControlledMission("Mission4", new[] { "Mission3", "Mission2" });
			var allMissions = new[] { mission1, mission2, dependantMission, dependantMission2 };
			foreach (var mission in allMissions)
				runner.RegisterMission(mission);

			// Act
			var finishedMission = runner.PerformAllMissions();
			foreach (var mission in allMissions)
				mission.FinishPerforming();
			finishedMission.Wait();
			finishedMission = runner.PerformAllMissions();

			// Assert
			Assert.AreEqual(1, dependantMission.RunCounter);
			mission1.FinishPerforming();
			mission2.FinishPerforming();
			dependantMission.FinishPerforming();
			Assert.IsFalse(finishedMission.Wait(100));

			dependantMission2.FinishPerforming();
			Assert.IsTrue(finishedMission.Wait(100));
			Assert.IsTrue(allMissions.All(t => t.RunCounter == 2));
		}

		[TestMethod]
		public async Task RunAllMissions_ShouldPassOnlyCorrectArgumentsToRelevantMissions()
		{
			// Arrange
			var missionRunner = new MissionRunner();
			var independeantMission = new ControlledMissionWithReturnValue<int>(6, "Mission", true);

			var level0Mission1 = new ControlledMissionWithReturnValue<string>("value", "FirstLevel0", true);
			var level0Mission2 = new ControlledMissionWithReturnValue<double>(5.5, "SecondLevel0", true);

			var midlevelMission = new ControlledMissionWithReturnValue<char>('r', "MidMission", new[] { "FirstLevel0" }, true);

			var finalMission = new ControlledMissionWithReturnValue<bool>(true, "FinalMission", new[] { "MidMission", "SecondLevel0" }, true);

			var allMissions = new IMission[] { independeantMission, level0Mission1, level0Mission2, midlevelMission, finalMission };
			foreach (var mission in allMissions)
				missionRunner.RegisterMission(mission);

			// Act
			await missionRunner.PerformAllMissions();

			// Assert
			Assert.AreEqual(0, independeantMission.Arguments.Count);
			Assert.AreEqual(0, level0Mission1.Arguments.Count);
			Assert.AreEqual(0, level0Mission2.Arguments.Count);
			Assert.AreEqual(1, midlevelMission.Arguments.Count);
			Assert.AreEqual(2, finalMission.Arguments.Count);

			Assert.AreEqual("value", midlevelMission.Arguments.Get("FirstLevel0"));

			Assert.AreEqual('r', finalMission.Arguments.Get("MidMission"));
			Assert.AreEqual(5.5, finalMission.Arguments.Get("SecondLevel0"));
		}

		[TestMethod]
		public void RunAllMissions_ShouldReturnAllValuesInFinalResult()
		{
			// Arrange
			var missionRunner = new MissionRunner();
			var independeantMission = new ControlledMissionWithReturnValue<int>(6, "Mission", true);

			var level0Mission1 = new ControlledMissionWithReturnValue<string>("value", "FirstLevel0", true);
			var level0Mission2 = new ControlledMissionWithReturnValue<double>(5.5, "SecondLevel0", true);

			var midlevelMission = new ControlledMissionWithReturnValue<char>('r', "MidMission", new[] { "FirstLevel0" }, true);

			var finalMission = new ControlledMissionWithReturnValue<bool>(true, "FinalMission", new[] { "MidMission", "SecondLevel0" }, true);

			var allMissions = new IMission[] { independeantMission, level0Mission1, level0Mission2, midlevelMission, finalMission };
			foreach (var mission in allMissions)
				missionRunner.RegisterMission(mission);

			// Act
			var runTask = missionRunner.PerformAllMissions();
			Assert.IsTrue(runTask.Wait(100));
			var finalHolder = runTask.Result;

			// Assert
			Assert.AreEqual(allMissions.Length, finalHolder.Count);

			Assert.AreEqual(6, finalHolder.Get("Mission"));
			Assert.AreEqual("value", finalHolder.Get("FirstLevel0"));
			Assert.AreEqual(5.5, finalHolder.Get("SecondLevel0"));
			Assert.AreEqual('r', finalHolder.Get("MidMission"));
			Assert.AreEqual(true, finalHolder.Get("FinalMission"));
		}

		#endregion

		#region GetMissionRunState

		[TestMethod]
		[ExpectedException(typeof(KeyNotFoundException))]
		public void GetMissionRunState_ShouldThrowExceptionForNonExistingMission()
		{
			// Arrange
			var runner = new MissionRunner();
			runner.RegisterMission(new ControlledMission("One Thing"));

			// Act
			runner.GetMissionRunState("Another Thing");
		}

		[TestMethod]
		public void GetMissionRunState_ReturnWaitingBeforeAnyRunnerWasRun()
		{
			// Arrange
			var runner = new MissionRunner();
			runner.RegisterMission(new ControlledMission("One Thing"));

			// Assert
			Assert.AreEqual(RunState.WAITING, runner.GetMissionRunState("One Thing"));
		}

		[TestMethod]
		public void GetMissionRunState_ReturnRunningWhileMissionIsInProgress()
		{
			// Arrange
			var runner = new MissionRunner();
			var mission = new ControlledMission("One Thing");
			runner.RegisterMission(mission);
			runner.PerformAllMissions();

			// Assert
			Assert.IsTrue(mission.IsPerformed);
			Assert.AreEqual(RunState.RUNNING, runner.GetMissionRunState("One Thing"));
			mission.FinishPerforming();
		}

		[TestMethod]
		public void GetMissionRunState_ReturnFinishedAfterTaskIsDone()
		{
			// Arrange
			var runner = new MissionRunner();
			var mission = new ControlledMission("One Thing");
			runner.RegisterMission(mission);
			mission.FinishPerforming();
			var performanceTask = runner.PerformAllMissions();
			performanceTask.Wait(100);

			// Assert
			Assert.AreEqual(RunState.FINISHED, runner.GetMissionRunState("One Thing"));
		}

		[TestMethod]
		public void GetMissionRunState_ReturnWaitingWhileWaitingOnADependency()
		{
			// Arrange
			var runner = new MissionRunner();
			var mission = new ControlledMission("One Thing");
			var dependantMission = new ControlledMission("Dep", new[] { "One Thing" }, true);
			runner.RegisterMission(mission);
			runner.RegisterMission(dependantMission);
			runner.PerformAllMissions();

			// Assert
			Assert.IsTrue(mission.IsPerformed);
			Assert.AreEqual(RunState.WAITING, runner.GetMissionRunState("Dep"));
			mission.FinishPerforming();
		}

		[TestMethod]
		public void GetMissionRunState_ReturnFinishedEvenIfOtherTasksAreWorking()
		{
			// Arrange
			var runner = new MissionRunner();
			var mission = new ControlledMission("One Thing");
			var dependantMission = new ControlledMission("Dep", new[] { "One Thing" });
			runner.RegisterMission(mission);
			runner.RegisterMission(dependantMission);
			runner.PerformAllMissions();
			mission.FinishPerforming();

			// Assert
			Assert.IsTrue(mission.IsPerformed);
			Assert.AreEqual(RunState.FINISHED, runner.GetMissionRunState("One Thing"));
		}

		#endregion

		#region Integration

		[TestMethod]
		public void Integration_ShouldOnlyFinishMissionWhenAllMissionsAreDone()
		{
			// Arrange
			var runner = new MissionRunner();
			var allMissions = GenerateManyRandomMissions(1000);
			foreach (var mission in allMissions)
				runner.RegisterMission(mission);

			// Act
			var isDone = runner.PerformAllMissions().Wait(4000);

			// Assert
			Assert.IsTrue(isDone);
			foreach (var mission in allMissions)
			{
				Assert.IsTrue(mission.IsPerformed);
				Assert.AreEqual(1, mission.RunCounter, string.Format("Mission {0} was run incorrect number of times!", mission.Name));
			}
		}

		[TestMethod]
		public void Integration_RunningManyMissionsTwiceShouldRunThemAllTwice()
		{
			// Arrange
			var runner = new MissionRunner();
			var allMissions = GenerateManyRandomMissions(10);
			foreach (var mission in allMissions)
				runner.RegisterMission(mission);

			// Act
			var isDone1 = runner.PerformAllMissions().Wait(4000);
			var isDone2 = runner.PerformAllMissions().Wait(4000);

			// Assert
			Assert.IsTrue(isDone1);
			//Assert.IsTrue(isDone2);
			foreach (var mission in allMissions)
				Assert.AreEqual(2, mission.RunCounter, string.Format("Mission {0} was run incorrect number of times!", mission.Name));
		}

		#endregion

		#region Private Methods

		private static List<ControlledMission> GenerateManyRandomMissions(int missionCount)
		{
			var allMissions = new List<ControlledMission>();
			var random = new Random();
			var freeMissions = random.Next(1, Math.Min(missionCount, 100));
			for (int i = 0; i < freeMissions; i++)
			{
				var controlledMission = new ControlledMission(i.ToString(), true);
				allMissions.Add(controlledMission);
			}

			var existingMissionCount = freeMissions;
			for (int i = freeMissions; i < missionCount; i++)
			{
				var dependencies = new List<string>();
				var dependencyCount = random.Next(10, 100);
				for (int j = 0; j < dependencyCount; j++)
					dependencies.Add(random.Next(0, existingMissionCount - 1).ToString());

				var controlledMission = new ControlledMission(i.ToString(), dependencies, true);
				allMissions.Add(controlledMission);

				existingMissionCount++;
			}

			return allMissions;
		}

		#endregion

		#region Private Classes

		private class ControlledMission : IMission
		{
			#region Fields

			private int _runCounter = 0;
			private readonly Semaphore _canFinishSemaphore;
			private readonly Semaphore _isPerformedSemaphore = new Semaphore(0, 1000);

			#endregion

			#region Properties

			public string Name { get; private set; }

			public IEnumerable<string> Dependencies { get; private set; }

			public int RunCounter { get { return _runCounter; } }

			public bool IsPerformed
			{
				get { return _isPerformedSemaphore.WaitOne(100); }
			}

			public IArgumentHolder Arguments { get; private set; }

			#endregion

			#region C'tor

			public ControlledMission(string name, IEnumerable<string> dependencies = null, bool finishAutomatically = false)
			{
				Name = name;
				Dependencies = dependencies ?? new string[] { };
				if (finishAutomatically)
					_canFinishSemaphore = null;
				else
					_canFinishSemaphore = new Semaphore(0, 1);
			}

			public ControlledMission(string name, bool finishAutomatically)
				: this(name, null, finishAutomatically)
			{
			}

			#endregion

			#region Public Methods

			public virtual object Perform(IArgumentHolder holder)
			{
				_isPerformedSemaphore.Release();

				Interlocked.Increment(ref _runCounter);
				Arguments = holder;

				if (_canFinishSemaphore != null)
					_canFinishSemaphore.WaitOne();

				return null;
			}

			public void FinishPerforming()
			{
				_canFinishSemaphore.Release();
			}

			public override string ToString()
			{
				return Name;
			}

			#endregion
		}

		private class ControlledMissionWithReturnValue<T> : ControlledMission
		{
			public T Value { get; private set; }

			public ControlledMissionWithReturnValue(T value, string name, IEnumerable<string> dependencies = null, bool finishAutomatically = false)
				: base(name, dependencies, finishAutomatically)
			{
				Value = value;
			}

			public ControlledMissionWithReturnValue(T value, string name, bool finishAutomatically)
				: this(value, name, null, finishAutomatically)
			{
			}

			public override object Perform(IArgumentHolder holder)
			{
				base.Perform(holder);

				return Value;
			}
		}

		#endregion
	}
}
