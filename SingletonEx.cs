namespace Pluton.Core {
	public static class SingletonEx {
		public static bool IsInitialized<T>() where T : class, ISingleton {
			return Singleton<T>.Instance != null;
		}
	}
}

