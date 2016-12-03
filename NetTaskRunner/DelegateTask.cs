using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTaskRunner
{
	public class DelegateTask : ITask
	{
		private Func<IArgumentHolder, object> _taskAction;

		public DelegateTask(string name, Func<IArgumentHolder, object> taskAction, IEnumerable<string> dependencies)
		{
			_taskAction = taskAction;
			Dependencies = dependencies;
			Name = name;
		}

		public IEnumerable<string> Dependencies { get; }

		public string Name { get; }

		public object Perform(IArgumentHolder argumentHolder)
		{
			return _taskAction(argumentHolder);
		}
	}
}
