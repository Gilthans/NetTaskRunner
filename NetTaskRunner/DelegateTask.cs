using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTaskRunner
{
	public class DelegateMission : IMission
	{
		private Func<IArgumentHolder, object> _missionAction;

		public DelegateMission(string name, Func<IArgumentHolder, object> missionAction, IEnumerable<string> dependencies)
		{
			_missionAction = missionAction;
			Dependencies = dependencies;
			Name = name;
		}

		public IEnumerable<string> Dependencies { get; }

		public string Name { get; }

		public object Perform(IArgumentHolder argumentHolder)
		{
			return _missionAction(argumentHolder);
		}
	}
}
