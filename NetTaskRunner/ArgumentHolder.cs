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

		private Dictionary<string, object> _argumentsByName = new Dictionary<string, object>();
		private Dictionary<Type, object> _argumentByType = new Dictionary<Type, object>();

		#endregion

		#region Public Properties

		public int Count
		{
			get
			{
				return _argumentsByName.Count;
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
			return _argumentsByName[name];
		}

		public void RegisterResult(string name, object argument)
		{
			if (argument == null)
				return;

			_argumentsByName.Add(name, argument);
			_argumentByType.Add(argument.GetType(), argument);
		}

		public void Clear()
		{
			_argumentsByName.Clear();
			_argumentByType.Clear();
		}

		#endregion
	}
}
