using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetTaskRunner
{
	public class MissionRunner
	{
		#region Fields

		private readonly Dictionary<string, MissionWrapper> _missionWrappers = new Dictionary<string, MissionWrapper>();

		#endregion

		#region C'tor

		public MissionRunner()
		{
		}

		#endregion

		#region Public Methods

		public void RegisterMission(IMission newMission)
		{
			if (newMission == null)
				throw new ArgumentNullException(nameof(newMission));
			var newMissionWrapper = new MissionWrapper(newMission);
			// Note that verifying the mission's dependencies exist here is critical: this ensures we can't have circular dependencies!
			UpdateMissionDependencies(newMissionWrapper);

			_missionWrappers.Add(newMission.Name, newMissionWrapper);
		}

		public Task<IArgumentHolder> PerformAllMissions()
		{
			IArgumentHolder globalArgumentHolder = new ArgumentHolder();
			if (_missionWrappers.Count == 0)
				return Task.FromResult(globalArgumentHolder);

			if (_missionWrappers.First().Value.RunState == RunState.FINISHED)
				ResetMissionWrappers();

			var finishingBarrier = new Barrier(_missionWrappers.Count + 1);

			// We have to do this sepearately and use ToList to make sure 
			// we don't have missions finishing while the loop is still running...
			var dependencyFreeMissions = _missionWrappers.Values.Where(mission => mission.UnmetDependencies == 0).ToList();

			foreach (var mission in dependencyFreeMissions)
				Task.Run(() => PerformMission(mission, globalArgumentHolder, finishingBarrier));

			return Task.Run(() =>
			{
				finishingBarrier.SignalAndWait();
				return globalArgumentHolder;
			});
		}

		public RunState GetMissionRunState(string missionName)
		{
			return _missionWrappers[missionName].RunState;
		}

		#endregion

		#region Private Methods

		private void UpdateMissionDependencies(MissionWrapper mission)
		{
			foreach (var dependency in mission.ActualMission.Dependencies)
			{
				MissionWrapper dependencyWrapper;
				if (!_missionWrappers.TryGetValue(dependency, out dependencyWrapper))
					throw new ArgumentException(string.Format("Task {0} depends on newTask {1} which was never registered!", mission.ActualMission.Name,
						dependency));

				dependencyWrapper.DependantMissions.Add(mission);
				mission.UnmetDependencies++;
			}
		}

		private void ResetMissionWrappers()
		{
			foreach (var mission in _missionWrappers.Values)
			{
				mission.UnmetDependencies = 0;
				mission.ArgumentHolder.Clear();
				mission.RunState = RunState.WAITING;
			}

			foreach (var mission in _missionWrappers.Values)
				foreach (var dependantMission in mission.DependantMissions)
					dependantMission.UnmetDependencies++;
		}

		private void PerformMission(MissionWrapper mission, IArgumentHolder globalArgumentHolder, Barrier finishingBarrier)
		{
			mission.RunState = RunState.RUNNING;
			var result = mission.ActualMission.Perform(mission.ArgumentHolder);
			globalArgumentHolder.RegisterResult(mission.ActualMission.Name, result);

			foreach (var dependantMission in mission.DependantMissions)
			{
				bool shouldPerform = false;
				lock (dependantMission)
				{
					dependantMission.ArgumentHolder.RegisterResult(mission.ActualMission.Name, result);
					dependantMission.UnmetDependencies--;
					if (dependantMission.UnmetDependencies == 0)
						shouldPerform = true;
				}

				if (shouldPerform)
				{
					var taskToPerform = dependantMission;
					Task.Run(() => PerformMission(taskToPerform, globalArgumentHolder, finishingBarrier));
				}
			}

			mission.RunState = RunState.FINISHED;
			finishingBarrier.RemoveParticipant();
		}

		private class MissionWrapper
		{
			public MissionWrapper(IMission mission)
			{
				ActualMission = mission;
				UnmetDependencies = 0;
				DependantMissions = new List<MissionWrapper>();
				ArgumentHolder = new ArgumentHolder();
				RunState = RunState.WAITING;
			}

			public RunState RunState { get; set; }

			public ArgumentHolder ArgumentHolder { get; }

			public IMission ActualMission { get; }

			public int UnmetDependencies { get; set; }

			public IList<MissionWrapper> DependantMissions { get; }

			public override string ToString()
			{
				return string.Format("{0} ({1} dependencies, {2} depenedent)", ActualMission.Name, UnmetDependencies, DependantMissions.Count);
			}
		}

		#endregion
	}
}
