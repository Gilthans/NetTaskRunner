using System.Collections.Generic;

namespace NetTaskRunner
{
	public interface ITask
	{
		string Name { get; }
		IEnumerable<string> Dependencies { get; }
		object Perform(IArgumentHolder argumentHolder);
	}
}