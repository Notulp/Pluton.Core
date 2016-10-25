using System;

namespace Pluton.Core
{
	public class Pre<T> : CountedInstance where T : Event
	{
		public T Event;

		public bool IsCanceled = false;

		public Pre(params object[] args)
		{
			Event = Activator.CreateInstance(typeof(T), args) as T;
		}

		public void Cancel()
		{
			IsCanceled = true;
		}
	}
}

