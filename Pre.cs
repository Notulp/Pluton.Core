using System;

namespace Pluton.Core
{
    public class Pre<T> : CountedInstance where T : Event
    {
        public T Event;

        public bool IsCanceled = false;
        public string Reason = "";

        public Pre(params object[] args)
        {
            Event = Activator.CreateInstance(typeof(T), args) as T;
        }

        public void Cancel()
        {
            IsCanceled = true;
        }

        public void Cancel(string reason)
        {
            IsCanceled = true;
            Reason = reason;
        }
    }
}
