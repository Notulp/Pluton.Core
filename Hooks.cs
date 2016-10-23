namespace Pluton.Core
{
	using System;
	using System.Reactive.Subjects;
	using System.Collections.Generic;

	using PluginLoaders;

	public class Hook : CountedInstance
	{
		~Hook()
		{
			if (hook != null)
				hook.Dispose();
		}

		public Hook(string method, Action<object[]> callback)
		{
			if (Hooks.GetInstance().HookNames.Contains(method))
				hook = Hooks.GetInstance().Subjects[method].Subscribe(callback);
			else
				throw new Exception($"Can't find the hook '{method}' to subscribe to.");
			Name = method;
		}

		public string Name;
		public IDisposable hook;
	}

	public class Hooks : Singleton<Hooks>, ISingleton
	{
		public void Initialize()
		{
		}

		public static bool Loaded = false;

		public List<string> HookNames = new List<string>();

		internal Dictionary<string, Subject<object[]>> Subjects = new Dictionary<string, Subject<object[]>>()
		{ { "On_AllPluginLoaded", new Subject<object[]>() } };

		public Dictionary<string, Subject<object[]>> CreateOrUpdateSubjects()
		{
			for (int i = 0; i < HookNames.Count; i++) {
				string hookName = HookNames[i];
				if (!Subjects.ContainsKey(hookName))
					Subjects.Add(hookName, new Subject<object[]>());
			}
			return Subjects;
		}

		public static void OnNext(string hook, params object[] args)
		{
			if (Loaded)
				Instance.Subjects[hook].OnNext(args);
			else
				Console.WriteLine($"[Hooks] Not calling method: {hook}, because Hooks is not initialized yet.");
		}

		public static Hook Subscribe(string hookname, Action<object[]> callback)
		{
			return new Hook(hookname, callback);
		}

		public static Hook Subscribe(string hookname, BasePlugin plugin)
		{
			return new Hook(hookname, args => plugin.Invoke(hookname, args));
		}
	}
}

