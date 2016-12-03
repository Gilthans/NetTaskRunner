using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetTaskRunner
{
	public class ArgumentHolder : IArgumentHolder
	{
		#region Fields

		private Dictionary<string, object> _argumentsByTaskName = new Dictionary<string, object>();
		private Dictionary<Type, object> _argumentByType = new Dictionary<Type, object>();

		#endregion

		#region Public Properties

		public int Count
		{
			get
			{
				return _argumentsByTaskName.Count;
			}
		}

		#endregion

		#region Public Methods

		public T Get<T>()
		{
			return (T)_argumentByType[typeof(T)];
		}

		public object Get(string name)
		{
			return _argumentsByTaskName[name];
		}

		public void RegisterResult(string sourceTask, object argument)
		{
			if (argument == null)
				return;

			_argumentsByTaskName.Add(sourceTask, argument);
			_argumentByType.Add(argument.GetType(), argument);
		}

		internal void Clear()
		{
			_argumentByType.Clear();
			_argumentByType.Clear();
		}

		#endregion
	}
}
