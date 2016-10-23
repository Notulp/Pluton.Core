namespace Pluton.Core
{
	using System;
	using System.Collections.Generic;
	using System.Timers;

	public class TimedEvent : CountedInstance
	{

		Dictionary<string, object> _args;
		readonly string _name;
		readonly Timer _timer;
		long lastTick;
		ulong _elapsedCount = 0;
		ulong elapsedReachedUInt64Max = 0;

		public delegate void TimedEventFireDelegate(TimedEvent evt);

		public event TimedEventFireDelegate OnFire;

		public TimedEvent(string name, double interval)
		{
			_name = name;
			_timer = new Timer();
			_timer.Interval = interval;
			_timer.Elapsed += _timer_Elapsed;
			_elapsedCount = 0;
		}

		public TimedEvent(string name, double interval, Dictionary<string, object> args)
			: this(name, interval)
		{
			_args = args;
		}

		void _timer_Elapsed(object sender, ElapsedEventArgs e)
		{
			if (OnFire != null)
				OnFire(this);

			if (_elapsedCount == UInt64.MaxValue) {
				_elapsedCount = 0;
				elapsedReachedUInt64Max++;
				if (elapsedReachedUInt64Max == UInt64.MaxValue)
					elapsedReachedUInt64Max = 0;
			}

			_elapsedCount++;
			lastTick = DateTime.UtcNow.Ticks;
		}

		public void Start()
		{
			_timer.Start();
			lastTick = DateTime.UtcNow.Ticks;
		}

		public void Stop() => _timer.Stop();

		public void Kill()
		{
			_timer.Stop();
			_timer.Dispose();
		}

		public Dictionary<string, object> Args {
			get { return _args; }
			set { _args = value; }
		}

		public double Interval {
			get { return _timer.Interval; }
			set { _timer.Interval = value; }
		}

		public string Name => _name;

		public double TimeLeft => (Interval - ((DateTime.UtcNow.Ticks - lastTick) / 0x2710L));

		public ulong ElapsedCount => _elapsedCount;

		public string Elapsed => elapsedReachedUInt64Max == 0 ? _elapsedCount.ToString() : elapsedReachedUInt64Max.ToString() + "x" + UInt64.MaxValue.ToString() + " + " + _elapsedCount;
	}
}

