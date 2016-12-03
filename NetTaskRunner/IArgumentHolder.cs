namespace NetTaskRunner
{
	public interface IArgumentHolder
	{
		int Count { get; }

		T Get<T>();

		object Get(string name);

		void RegisterResult(string name, object argument);
	}
}