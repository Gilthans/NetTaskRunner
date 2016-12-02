using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTaskRunner
{
	public class DelegateTask : ITask
	{
		private Action _taskAction;

		public DelegateTask(string name, Action taskAction, IEnumerable<string> dependencies)
		{
			_taskAction = taskAction;
			Dependencies = dependencies;
			Name = name;
		}

		public IEnumerable<string> Dependencies { get; }

		public string Name { get; }

		public void Perform()
		{
			_taskAction();
		}
	}
}
