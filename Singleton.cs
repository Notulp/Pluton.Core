namespace Pluton.Core
{
	using System;
	using System.Linq;
	using System.Collections.Generic;

	public abstract class Singleton<T> : CountedInstance, ISingleton<T> where T : class, ISingleton
	{
		public static List<Func<bool>> dependencies = new List<Func<bool>>() { () => true };

		public List<Func<bool>> Dependencies => dependencies;

		public static T Instance;

		public bool CheckDependencies()
		{
			return (from dependencies in Dependencies
			        select dependencies()).All((result) => result == true);
		}

		public static T GetInstance()
		{
			if (!SingletonEx.IsInitialized<T>())
				throw new NullReferenceException($"Singleton<{typeof(T).Name}>.GetInstance() failed. The requested type singleton is not initialized and for that reason it's instance is null.");
			
			return Instance;
		}

		public static void SetInstance<T1>() where T1 : class, T
		{
			Instance = Singleton<T1>.GetInstance();
		}

		static Singleton()
		{
			Instance = Activator.CreateInstance<T>();
			if (Instance.CheckDependencies())
				Instance.Initialize();
			else {
				UnityEngine.Debug.LogWarning($"Couldn't initialite Singleton<{typeof(T).Name}>, is one of it's dependencies missing?");
				Instance = null;
			}
		}
	}
}

