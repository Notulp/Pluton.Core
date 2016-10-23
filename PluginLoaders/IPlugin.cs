using System;

namespace Pluton.Core.PluginLoaders {
	public interface IPlugin {
		string FormatException(Exception ex);

		object GetGlobalObject(string id);

		string GetPluginPath();

		object Invoke(string method, params object[] args);

		void Load(string code);
	}

	public enum PluginState : sbyte {
		FailedToLoad = -1,
		NotLoaded = 0,
		Loaded = 1,
		HashNotFound = 2
	}

	public struct PluginType {
		public Type Type;
		public string Extension;

		public static implicit operator string(PluginType dis) => dis.Type.Name;

		public static PluginType FromType<T>() where T : BasePlugin {
			return new PluginType() {
				Type = typeof(T),
				Extension = PluginLoaderHelper.GetExtension<T>()
			};
		}

		public override string ToString() => Type.Name;
	}
}

